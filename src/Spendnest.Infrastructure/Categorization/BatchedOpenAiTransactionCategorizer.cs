using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Spendnest.Core.Categorization;
using Spendnest.Core.Credentials;
using Spendnest.Core.Categories;
using Spendnest.Core.Transactions;

namespace Spendnest.Infrastructure.Categorization;

/// <summary>
/// Uses the saved OpenAI key to run OpenAI transaction categorization in batches.
/// </summary>
public sealed class BatchedOpenAiTransactionCategorizer : ITransactionCategorizer
{
    private readonly ICredentialStore credentialStore;
    private readonly HttpClient httpClient;
    private readonly OpenAiCategorizerOptions options;
    private readonly ILogger<BatchedOpenAiTransactionCategorizer> logger;
    private readonly ILoggerFactory loggerFactory;

    public BatchedOpenAiTransactionCategorizer(
        ICredentialStore credentialStore,
        HttpClient httpClient,
        OpenAiCategorizerOptions options,
        ILogger<BatchedOpenAiTransactionCategorizer>? logger = null,
        ILoggerFactory? loggerFactory = null)
    {
        this.credentialStore = credentialStore;
        this.httpClient = httpClient;
        this.options = options;
        this.logger = logger ?? NullLogger<BatchedOpenAiTransactionCategorizer>.Instance;
        this.loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
    }

    public async Task<IReadOnlyList<TransactionCategorization>> CategorizeAsync(
        IReadOnlyList<Transaction> transactions,
        CancellationToken cancellationToken)
    {
        var apiKey = await GetRequiredApiKeyAsync(cancellationToken).ConfigureAwait(false);
        var batchSize = GetBatchSize();
        var categorizer = CreateCategorizer(apiKey);
        var results = new List<TransactionCategorization>();

        logger.LogInformation(
            "Starting batched OpenAI categorization for {TransactionCount} transactions in batches of {BatchSize}.",
            transactions.Count,
            batchSize);
        var batchNumber = 0;
        foreach (var batch in transactions.Chunk(batchSize))
        {
            batchNumber++;
            var batchResults = await CategorizeBatchAsync(
                categorizer,
                batch,
                batchNumber,
                cancellationToken).ConfigureAwait(false);
            results.AddRange(batchResults);
        }

        logger.LogInformation(
            "Finished batched OpenAI categorization with {ResultCount} total results.",
            results.Count);

        return results;
    }

    private async Task<string> GetRequiredApiKeyAsync(CancellationToken cancellationToken)
    {
        var apiKey = await credentialStore.GetStringAsync(CredentialKeys.OpenAiApiKey, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            return apiKey;
        }

        logger.LogWarning("Batched OpenAI categorization skipped because no API key is configured.");
        throw new AiCategorizationUnavailableException("OpenAI API key is required for OpenAI categorization.");
    }

    private int GetBatchSize()
    {
        return Math.Max(1, options.MaxTransactionsPerRequest);
    }

    private OpenAiTransactionCategorizer CreateCategorizer(string apiKey)
    {
        return new OpenAiTransactionCategorizer(
            httpClient,
            CreateCategorizerOptions(apiKey),
            loggerFactory.CreateLogger<OpenAiTransactionCategorizer>(),
            loggerFactory.CreateLogger<OpenAiClient>());
    }

    private OpenAiCategorizerOptions CreateCategorizerOptions(string apiKey)
    {
        return new OpenAiCategorizerOptions
        {
            ApiKey = apiKey,
            Endpoint = options.Endpoint,
            Model = options.Model,
            ReviewConfidenceThreshold = options.ReviewConfidenceThreshold,
            RequestTimeout = options.RequestTimeout,
            MaxTransactionsPerRequest = options.MaxTransactionsPerRequest
        };
    }

    /// <summary>
    /// Sends one batch, then sends any transactions the response left out once more. Transactions still
    /// missing after that get no result here and are reported as unresolved by the caller.
    /// </summary>
    private async Task<IReadOnlyList<TransactionCategorization>> CategorizeBatchAsync(
        ITransactionCategorizer categorizer,
        Transaction[] batch,
        int batchNumber,
        CancellationToken cancellationToken)
    {
        var results = (await CategorizeBatchWithTimeoutRetryAsync(categorizer, batch, batchNumber, cancellationToken).ConfigureAwait(false))
            .ToList();
        var missing = FindMissingTransactions(batch, results);
        if (missing.Length == 0)
        {
            return results;
        }

        logger.LogWarning(
            "OpenAI categorization batch {BatchNumber} returned no result for {MissingCount} transactions; retrying them: {Descriptions}",
            batchNumber,
            missing.Length,
            DescribeTransactions(missing));
        var retryResults = await TryCategorizeBatchAsync(categorizer, missing, batchNumber, cancellationToken).ConfigureAwait(false);
        results.AddRange(retryResults ?? CreateTimedOutBatchResults(missing, batchNumber));

        var stillMissing = FindMissingTransactions(missing, results);
        if (stillMissing.Length > 0)
        {
            logger.LogWarning(
                "OpenAI categorization batch {BatchNumber} still returned no result for {MissingCount} transactions after retrying: {Descriptions}",
                batchNumber,
                stillMissing.Length,
                DescribeTransactions(stillMissing));
        }

        return results;
    }

    private static Transaction[] FindMissingTransactions(
        IReadOnlyList<Transaction> transactions,
        IReadOnlyList<TransactionCategorization> results)
    {
        var resultIds = results.Select(result => result.TransactionId).ToHashSet();
        return transactions
            .Where(transaction => !resultIds.Contains(transaction.Id))
            .ToArray();
    }

    private static string DescribeTransactions(IReadOnlyList<Transaction> transactions)
    {
        return string.Join("; ", transactions.Select(transaction => transaction.OriginalDescription));
    }

    /// <summary>
    /// Sends one batch. A batch that times out is retried once as two halves; a half that times out
    /// again is returned as unresolved.
    /// </summary>
    private async Task<IReadOnlyList<TransactionCategorization>> CategorizeBatchWithTimeoutRetryAsync(
        ITransactionCategorizer categorizer,
        Transaction[] batch,
        int batchNumber,
        CancellationToken cancellationToken)
    {
        var batchResults = await TryCategorizeBatchAsync(categorizer, batch, batchNumber, cancellationToken).ConfigureAwait(false);
        if (batchResults is not null)
        {
            return batchResults;
        }

        logger.LogInformation(
            "Retrying OpenAI categorization batch {BatchNumber} as two halves.",
            batchNumber);
        var results = new List<TransactionCategorization>(batch.Length);
        foreach (var half in SplitInHalves(batch))
        {
            var halfResults = await TryCategorizeBatchAsync(categorizer, half, batchNumber, cancellationToken).ConfigureAwait(false);
            results.AddRange(halfResults ?? CreateTimedOutBatchResults(half, batchNumber));
        }

        return results;
    }

    private static Transaction[][] SplitInHalves(Transaction[] batch)
    {
        if (batch.Length < 2)
        {
            return [batch];
        }

        var firstHalfLength = (batch.Length + 1) / 2;
        return [batch[..firstHalfLength], batch[firstHalfLength..]];
    }

    /// <summary>
    /// Returns the batch results, or null when the request timed out.
    /// </summary>
    private async Task<IReadOnlyList<TransactionCategorization>?> TryCategorizeBatchAsync(
        ITransactionCategorizer categorizer,
        Transaction[] batch,
        int batchNumber,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.RequestTimeout);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            logger.LogInformation(
                "Sending OpenAI categorization batch {BatchNumber} with {TransactionCount} transactions.",
                batchNumber,
                batch.Length);
            var batchResults = await categorizer.CategorizeAsync(batch, timeout.Token).ConfigureAwait(false);
            logger.LogInformation(
                "Completed OpenAI categorization batch {BatchNumber}; received {ResultCount} results.",
                batchNumber,
                batchResults.Count);

            return batchResults;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && timeout.IsCancellationRequested)
        {
            LogBatchTimeout(batch, batchNumber, stopwatch.Elapsed, "the request timeout elapsed");
            return null;
        }
        catch (TimeoutException exception)
        {
            LogBatchTimeout(batch, batchNumber, stopwatch.Elapsed, exception.Message);
            return null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception,
                "OpenAI categorization batch {BatchNumber} failed after {ElapsedMilliseconds} ms for {TransactionCount} transactions: {ExceptionType}: {Reason}. Transactions: {Descriptions}",
                batchNumber,
                (long)stopwatch.Elapsed.TotalMilliseconds,
                batch.Length,
                exception.GetType().Name,
                exception.Message,
                DescribeTransactions(batch));
            throw;
        }
    }

    private void LogBatchTimeout(
        IReadOnlyList<Transaction> batch,
        int batchNumber,
        TimeSpan elapsed,
        string reason)
    {
        logger.LogWarning(
            "OpenAI categorization batch {BatchNumber} timed out after {ElapsedMilliseconds} ms (limit {TimeoutSeconds} s) for {TransactionCount} transactions: {Reason}. Transactions: {Descriptions}",
            batchNumber,
            (long)elapsed.TotalMilliseconds,
            options.RequestTimeout.TotalSeconds,
            batch.Count,
            reason,
            DescribeTransactions(batch));
    }

    private IReadOnlyList<TransactionCategorization> CreateTimedOutBatchResults(
        IReadOnlyList<Transaction> batch,
        int batchNumber)
    {
        logger.LogWarning(
            "OpenAI categorization batch {BatchNumber} gave up after retrying; {TransactionCount} transactions are unresolved: {Descriptions}",
            batchNumber,
            batch.Count,
            DescribeTransactions(batch));

        return CreateUnresolvedResults(batch, "AI categorization timed out.");
    }

    private static IReadOnlyList<TransactionCategorization> CreateUnresolvedResults(
        IReadOnlyList<Transaction> transactions,
        string explanation)
    {
        return transactions
            .Select(transaction => new TransactionCategorization(
                transaction.Id,
                BuiltInCategoryIds.Other,
                0m,
                true,
                CategorizationSource.Unresolved,
                explanation))
            .ToArray();
    }
}
