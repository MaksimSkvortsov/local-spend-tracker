using Spendnest.Core.Categorization;
using Spendnest.Core.Credentials;
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

        var categorizer = new OpenAiTransactionCategorizer(
            httpClient,
            new OpenAiCategorizerOptions
            {
                ApiKey = apiKey,
                Endpoint = options.Endpoint,
                Model = options.Model,
                ReviewConfidenceThreshold = options.ReviewConfidenceThreshold,
                RequestTimeout = options.RequestTimeout
            });

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.RequestTimeout);

        try
        {
            return await categorizer.CategorizeAsync(transactions, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && timeout.IsCancellationRequested)
        {
            throw new TimeoutException($"OpenAI categorization timed out after {options.RequestTimeout.TotalSeconds:0} seconds.");
        }
    }
}
