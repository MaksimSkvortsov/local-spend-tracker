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
        throw new InvalidOperationException("OpenAI API key is required for OpenAI categorization.");
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

    private async Task<IReadOnlyList<TransactionCategorization>> CategorizeBatchAsync(
        ITransactionCategorizer categorizer,
        Transaction[] batch,
        int batchNumber,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.RequestTimeout);

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
            return CreateTimedOutBatchResults(batch, batchNumber);
        }
        catch (TimeoutException)
        {
            return CreateTimedOutBatchResults(batch, batchNumber);
        }
    }

    private IReadOnlyList<TransactionCategorization> CreateTimedOutBatchResults(
        IReadOnlyList<Transaction> batch,
        int batchNumber)
    {
        logger.LogWarning(
            "OpenAI categorization batch {BatchNumber} timed out after {TimeoutSeconds} seconds.",
            batchNumber,
            options.RequestTimeout.TotalSeconds);

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
