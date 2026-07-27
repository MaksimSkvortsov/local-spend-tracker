using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;

namespace Spendnest.Infrastructure.Categorization;

/// <summary>
/// Applies stored local rules and deterministic keywords before AI is used.
/// </summary>
public sealed class LocalTransactionCategorizer : ILocalTransactionCategorizer
{
    private readonly ICategoryRuleRepository ruleRepository;
    private readonly ITransactionCategoryMapper keywordMapper;

    public LocalTransactionCategorizer(
        ICategoryRuleRepository ruleRepository,
        ITransactionCategoryMapper keywordMapper)
    {
        this.ruleRepository = ruleRepository;
        this.keywordMapper = keywordMapper;
    }

    public async Task<IReadOnlyList<TransactionCategorization>> CategorizeKnownAsync(
        IReadOnlyList<Transaction> transactions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transactions);
        cancellationToken.ThrowIfCancellationRequested();

        var rules = await ruleRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        var results = new List<TransactionCategorization>();

        foreach (var transaction in transactions)
        {
            var categoryId = FindRuleCategoryId(transaction, rules)
                ?? FindKeywordCategoryId(transaction);

            if (categoryId is null)
            {
                continue;
            }

            results.Add(new TransactionCategorization(
                transaction.Id,
                categoryId.Value,
                1m,
                false,
                CategorizationSource.LocalRules,
                "Matched local categorization knowledge."));
        }

        return results;
    }

    private static int? FindRuleCategoryId(
        Transaction transaction,
        IReadOnlyList<CategoryRule> rules)
    {
        var description = Normalize(transaction.OriginalDescription);

        foreach (var rule in rules.OrderBy(rule => rule.MatchType))
        {
            var pattern = Normalize(rule.Pattern);
            var isMatch = rule.MatchType switch
            {
                CategoryRuleMatchType.Exact => description == pattern,
                CategoryRuleMatchType.Contains => description.Contains(pattern),
                _ => false
            };

            if (isMatch)
            {
                return rule.CategoryId;
            }
        }

        return null;
    }

    private int? FindKeywordCategoryId(Transaction transaction)
    {
        var categoryId = keywordMapper.MapCategoryId(transaction);

        return categoryId is BuiltInCategoryIds.Other
            ? null
            : categoryId;
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToUpperInvariant();
    }
}
