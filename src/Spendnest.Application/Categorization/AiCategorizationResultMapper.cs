using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;

namespace Spendnest.Application.Categorization;

public sealed class AiCategorizationResultMapper
{
    private readonly ITransactionMerchantCodeResolver merchantCodeResolver;

    public AiCategorizationResultMapper(ITransactionMerchantCodeResolver merchantCodeResolver)
    {
        this.merchantCodeResolver = merchantCodeResolver;
    }

    public IReadOnlyList<TransactionCategorization> MapToUnresolvedTransactions(
        IReadOnlyList<TransactionCategorization> representativeResults,
        IReadOnlyList<Transaction> representativeTransactions,
        IReadOnlyList<Transaction> unresolvedTransactions)
    {
        ArgumentNullException.ThrowIfNull(representativeResults);
        ArgumentNullException.ThrowIfNull(representativeTransactions);
        ArgumentNullException.ThrowIfNull(unresolvedTransactions);

        var expandedResults = ExpandRepresentativeResults(
            representativeResults,
            representativeTransactions,
            unresolvedTransactions);
        var validTransactionIds = unresolvedTransactions.Select(transaction => transaction.Id).ToHashSet();
        var acceptedResults = expandedResults
            .Where(result => validTransactionIds.Contains(result.TransactionId))
            .Select(result => IsSupportedCategory(result.CategoryId)
                ? result
                : CreateInvalidResult(result))
            .ToArray();
        var acceptedResultIds = acceptedResults.Select(result => result.TransactionId).ToHashSet();
        var missingResults = unresolvedTransactions
            .Where(transaction => !acceptedResultIds.Contains(transaction.Id))
            .Select(transaction => CreateUnresolvedResult(transaction, "No category result was returned."))
            .ToArray();

        return acceptedResults
            .Concat(missingResults)
            .ToArray();
    }

    private IReadOnlyList<TransactionCategorization> ExpandRepresentativeResults(
        IReadOnlyList<TransactionCategorization> representativeResults,
        IReadOnlyList<Transaction> representativeTransactions,
        IReadOnlyList<Transaction> unresolvedTransactions)
    {
        var representativeTransactionsById = representativeTransactions.ToDictionary(transaction => transaction.Id);
        var representativeResultsByMerchantCode = representativeResults
            .Where(result => representativeTransactionsById.ContainsKey(result.TransactionId))
            .ToDictionary(
                result => merchantCodeResolver.Resolve(representativeTransactionsById[result.TransactionId]),
                result => result,
                StringComparer.Ordinal);
        var learnedPrefixResults = representativeResults
            .Where(result => representativeTransactionsById.ContainsKey(result.TransactionId)
                && !result.NeedsReview
                && result.Source is not CategorizationSource.Unresolved
                && result.CategoryId != BuiltInCategoryIds.Other
                && !string.IsNullOrWhiteSpace(result.LearnedRulePrefix))
            .Select(result => new LearnedPrefixResult(
                result,
                NormalizeRulePrefix(result.LearnedRulePrefix),
                NormalizeRulePrefix(merchantCodeResolver.Resolve(representativeTransactionsById[result.TransactionId]))))
            .Where(item => item.Prefix.Length > 0
                && item.MerchantCode.StartsWith(item.Prefix, StringComparison.Ordinal))
            .OrderByDescending(item => item.Prefix.Length)
            .ToArray();
        var results = new List<TransactionCategorization>();

        foreach (var transaction in unresolvedTransactions)
        {
            var merchantCode = NormalizeRulePrefix(merchantCodeResolver.Resolve(transaction));
            var learnedMatch = learnedPrefixResults
                .FirstOrDefault(item => merchantCode.StartsWith(item.Prefix, StringComparison.Ordinal));

            if (learnedMatch is not null)
            {
                results.Add(CopyResultForTransaction(learnedMatch.Result, transaction.Id, learnedMatch.Prefix));
                continue;
            }

            if (representativeResultsByMerchantCode.TryGetValue(merchantCode, out var representativeResult))
            {
                results.Add(CopyResultForTransaction(representativeResult, transaction.Id, representativeResult.LearnedRulePrefix));
            }
        }

        return results;
    }

    private static bool IsSupportedCategory(int categoryId)
    {
        return BuiltInCategories.All.Any(category => category.Id == categoryId);
    }

    private static TransactionCategorization CopyResultForTransaction(
        TransactionCategorization result,
        Guid transactionId,
        string? learnedRulePrefix)
    {
        return result with
        {
            TransactionId = transactionId,
            LearnedRulePrefix = learnedRulePrefix
        };
    }

    private static string NormalizeRulePrefix(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();
    }

    private static TransactionCategorization CreateInvalidResult(TransactionCategorization result)
    {
        return result with
        {
            CategoryId = BuiltInCategoryIds.Other,
            Confidence = 0m,
            NeedsReview = true,
            Source = CategorizationSource.Unresolved,
            Explanation = $"Rejected unsupported category id '{result.CategoryId}'."
        };
    }

    private static TransactionCategorization CreateUnresolvedResult(
        Transaction transaction,
        string explanation)
    {
        return new TransactionCategorization(
            transaction.Id,
            BuiltInCategoryIds.Other,
            0m,
            true,
            CategorizationSource.Unresolved,
            explanation);
    }

    private sealed record LearnedPrefixResult(
        TransactionCategorization Result,
        string Prefix,
        string MerchantCode);
}
