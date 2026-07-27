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

    public StoredOpenAiTransactionCategorizer(
        ICredentialStore credentialStore,
        HttpClient httpClient,
        OpenAiCategorizerOptions options)
    {
        this.credentialStore = credentialStore;
        this.httpClient = httpClient;
        this.options = options;
    }

    public async Task<IReadOnlyList<TransactionCategorization>> CategorizeAsync(
        IReadOnlyList<Transaction> transactions,
        CancellationToken cancellationToken)
    {
        var apiKey = await credentialStore.GetStringAsync(CredentialKeys.OpenAiApiKey, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
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
        var categorizer = new OpenAiTransactionCategorizer(httpClient, categorizerOptions);

        foreach (var batch in transactions.Chunk(batchSize))
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(options.RequestTimeout);

            try
            {
                var batchResults = await categorizer.CategorizeAsync(batch, timeout.Token).ConfigureAwait(false);
                results.AddRange(batchResults);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && timeout.IsCancellationRequested)
            {
                results.AddRange(CreateUnresolvedResults(batch, "AI categorization timed out."));
            }
            catch (TimeoutException)
            {
                results.AddRange(CreateUnresolvedResults(batch, "AI categorization timed out."));
            }
        }

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
