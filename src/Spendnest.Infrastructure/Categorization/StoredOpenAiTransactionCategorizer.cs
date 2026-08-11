using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Spendnest.Core.Categorization;
using Spendnest.Core.Credentials;
using Spendnest.Core.Categories;
using Spendnest.Core.Transactions;

namespace Spendnest.Infrastructure.Categorization;

/// <summary>
/// Uses the saved OpenAI key to run OpenAI transaction categorization.
/// </summary>
public sealed class StoredOpenAiTransactionCategorizer : ITransactionCategorizer
{
    private readonly ICredentialStore credentialStore;
    private readonly HttpClient httpClient;
    private readonly OpenAiCategorizerOptions options;
    private readonly ILogger<StoredOpenAiTransactionCategorizer> logger;
    private readonly ILoggerFactory loggerFactory;

    public StoredOpenAiTransactionCategorizer(
        ICredentialStore credentialStore,
        HttpClient httpClient,
        OpenAiCategorizerOptions options,
        ILogger<StoredOpenAiTransactionCategorizer>? logger = null,
        ILoggerFactory? loggerFactory = null)
    {
        this.credentialStore = credentialStore;
        this.httpClient = httpClient;
        this.options = options;
        this.logger = logger ?? NullLogger<StoredOpenAiTransactionCategorizer>.Instance;
        this.loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
    }

    public async Task<IReadOnlyList<TransactionCategorization>> CategorizeAsync(
        IReadOnlyList<Transaction> transactions,
        CancellationToken cancellationToken)
    {
        var apiKey = await credentialStore.GetStringAsync(CredentialKeys.OpenAiApiKey, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("Stored OpenAI categorization skipped because no API key is configured.");
            throw new InvalidOperationException("OpenAI API key is required for OpenAI categorization.");
        }

        var batchSize = Math.Max(1, options.MaxTransactionsPerRequest);
        var results = new List<TransactionCategorization>();
        var categorizerOptions = new OpenAiCategorizerOptions
        {
            ApiKey = apiKey,
            Endpoint = options.Endpoint,
            Model = options.Model,
            ReviewConfidenceThreshold = options.ReviewConfidenceThreshold,
            RequestTimeout = options.RequestTimeout,
            MaxTransactionsPerRequest = options.MaxTransactionsPerRequest
        };
        var categorizer = new OpenAiTransactionCategorizer(
            httpClient,
            categorizerOptions,
            loggerFactory.CreateLogger<OpenAiTransactionCategorizer>());

        logger.LogInformation(
            "Starting stored OpenAI categorization for {TransactionCount} transactions in batches of {BatchSize}.",
            transactions.Count,
            batchSize);
        var batchNumber = 0;
        foreach (var batch in transactions.Chunk(batchSize))
        {
            batchNumber++;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(options.RequestTimeout);

            try
            {
                logger.LogInformation(
                    "Sending OpenAI categorization batch {BatchNumber} with {TransactionCount} transactions.",
                    batchNumber,
                    batch.Length);
                var batchResults = await categorizer.CategorizeAsync(batch, timeout.Token).ConfigureAwait(false);
                results.AddRange(batchResults);
                logger.LogInformation(
                    "Completed OpenAI categorization batch {BatchNumber}; received {ResultCount} results.",
                    batchNumber,
                    batchResults.Count);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && timeout.IsCancellationRequested)
            {
                logger.LogWarning(
                    "OpenAI categorization batch {BatchNumber} timed out after {TimeoutSeconds} seconds.",
                    batchNumber,
                    options.RequestTimeout.TotalSeconds);
                results.AddRange(CreateUnresolvedResults(batch, "AI categorization timed out."));
            }
            catch (TimeoutException)
            {
                logger.LogWarning(
                    "OpenAI categorization batch {BatchNumber} timed out after {TimeoutSeconds} seconds.",
                    batchNumber,
                    options.RequestTimeout.TotalSeconds);
                results.AddRange(CreateUnresolvedResults(batch, "AI categorization timed out."));
            }
        }

        logger.LogInformation(
            "Finished stored OpenAI categorization with {ResultCount} total results.",
            results.Count);

        return results;
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
