using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;

namespace Spendnest.Infrastructure.Categorization;

/// <summary>
/// Applies stored local rules before AI is used.
/// </summary>
public sealed class LocalTransactionCategorizer : ILocalTransactionCategorizer
{
    private readonly ICategoryRuleRepository ruleRepository;
    private readonly ITransactionMerchantCodeResolver merchantCodeResolver;

    public LocalTransactionCategorizer(
        ICategoryRuleRepository ruleRepository,
        ITransactionMerchantCodeResolver merchantCodeResolver)
    {
        this.ruleRepository = ruleRepository;
        this.merchantCodeResolver = merchantCodeResolver;
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
            var categoryId = FindRuleCategoryId(transaction, rules);

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

    private int? FindRuleCategoryId(
        Transaction transaction,
        IReadOnlyList<CategoryRule> rules)
    {
        var description = Normalize(transaction.OriginalDescription);
        var merchantCode = merchantCodeResolver.Resolve(transaction);

        foreach (var rule in rules
            .OrderBy(rule => rule.MatchType)
            .ThenByDescending(rule => Normalize(rule.Pattern).Length))
        {
            var pattern = Normalize(rule.Pattern);
            var isMatch = rule.MatchType switch
            {
                CategoryRuleMatchType.Exact => merchantCode == pattern,
                CategoryRuleMatchType.Prefix => merchantCode.StartsWith(pattern, StringComparison.Ordinal),
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

    private static string Normalize(string value)
    {
        return value.Trim().ToUpperInvariant();
    }
}
