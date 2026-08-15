using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;

namespace Spendnest.Infrastructure.Categorization;

/// <summary>
/// Categorizes transactions with OpenAI while validating every returned category.
/// </summary>
public sealed class OpenAiTransactionCategorizer : ITransactionCategorizer
{
    private readonly OpenAiCategorizerOptions options;
    private readonly OpenAiClient client;
    private readonly OpenAiTransactionCategorizationRequestBuilder requestBuilder;
    private readonly OpenAiTransactionCategorizationResponseReader responseReader;
    private readonly ILogger<OpenAiTransactionCategorizer> logger;

    public OpenAiTransactionCategorizer(
        HttpClient httpClient,
        OpenAiCategorizerOptions options,
        ILogger<OpenAiTransactionCategorizer>? logger = null,
        ILogger<OpenAiClient>? clientLogger = null,
        OpenAiClient? client = null,
        OpenAiTransactionCategorizationRequestBuilder? requestBuilder = null,
        OpenAiTransactionCategorizationResponseReader? responseReader = null)
    {
        this.options = options;
        this.client = client ?? new OpenAiClient(httpClient, options, clientLogger);
        this.requestBuilder = requestBuilder ?? new OpenAiTransactionCategorizationRequestBuilder(options);
        this.responseReader = responseReader ?? new OpenAiTransactionCategorizationResponseReader(options);
        this.logger = logger ?? NullLogger<OpenAiTransactionCategorizer>.Instance;
    }

    public async Task<IReadOnlyList<TransactionCategorization>> CategorizeAsync(
        IReadOnlyList<Transaction> transactions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transactions);
        cancellationToken.ThrowIfCancellationRequested();

        if (transactions.Count == 0)
        {
            return [];
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            logger.LogWarning("OpenAI categorization skipped because no API key is configured.");
            throw new InvalidOperationException("OpenAI API key is required for OpenAI categorization.");
        }

        logger.LogInformation(
            "Sending OpenAI categorization request for {TransactionCount} transactions using model {Model}.",
            transactions.Count,
            options.Model);
        var outputText = await client
            .SendResponsesRequestAsync(requestBuilder.Build(transactions), cancellationToken)
            .ConfigureAwait(false);

        var categorizations = responseReader.Read(outputText, transactions);
        logger.LogInformation(
            "Parsed {CategorizationCount} OpenAI categorizations for {TransactionCount} transactions; {NeedsReviewCount} need review.",
            categorizations.Count,
            transactions.Count,
            categorizations.Count(categorization => categorization.NeedsReview));

        return categorizations;
    }
}
