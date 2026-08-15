using System.Text.Json;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;

namespace Spendnest.Infrastructure.Categorization;

public sealed class OpenAiTransactionCategorizationResponseReader
{
    private readonly OpenAiCategorizerOptions options;

    public OpenAiTransactionCategorizationResponseReader(OpenAiCategorizerOptions options)
    {
        this.options = options;
    }

    public IReadOnlyList<TransactionCategorization> Read(
        string outputText,
        IReadOnlyList<Transaction> transactions)
    {
        using var outputDocument = JsonDocument.Parse(outputText);
        var outputRoot = outputDocument.RootElement;
        var transactionIds = transactions.Select(transaction => transaction.Id).ToHashSet();
        var categoryIds = BuiltInCategories.All.Select(category => category.Id).ToHashSet();

        if (!outputRoot.TryGetProperty("items", out var itemsElement) || itemsElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("OpenAI categorization response did not include an items array.");
        }

        var categorizations = new List<TransactionCategorization>();

        foreach (var item in itemsElement.EnumerateArray())
        {
            var transactionId = Guid.Parse(item.GetProperty("transactionId").GetString() ?? string.Empty);
            if (!transactionIds.Contains(transactionId))
            {
                continue;
            }

            var categoryId = item.GetProperty("categoryId").GetInt32();
            if (!categoryIds.Contains(categoryId))
            {
                throw new InvalidOperationException($"OpenAI returned unsupported category id '{categoryId}'.");
            }

            var rulePrefix = item.GetProperty("rulePrefix").GetString() ?? string.Empty;
            var confidence = item.GetProperty("confidence").GetDecimal();
            var explanation = item.GetProperty("explanation").GetString() ?? string.Empty;

            categorizations.Add(new TransactionCategorization(
                transactionId,
                categoryId,
                confidence,
                confidence < options.ReviewConfidenceThreshold,
                CategorizationSource.Ai,
                explanation,
                rulePrefix));
        }

        return categorizations;
    }
}
