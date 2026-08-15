using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;

namespace Spendnest.Infrastructure.Categorization;

public sealed class LocalCategoryRuleMatcher
{
    private readonly ITransactionMerchantCodeResolver merchantCodeResolver;

    public LocalCategoryRuleMatcher(ITransactionMerchantCodeResolver merchantCodeResolver)
    {
        this.merchantCodeResolver = merchantCodeResolver;
    }

    public CategoryRule? FindMatch(
        Transaction transaction,
        IReadOnlyList<CategoryRule> rules)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(rules);

        var description = Normalize(transaction.OriginalDescription);
        var merchantCode = merchantCodeResolver.Resolve(transaction);

        foreach (var rule in OrderByMatchPriority(rules))
        {
            if (IsMatch(rule, merchantCode, description))
            {
                return rule;
            }
        }

        return null;
    }

    private static IOrderedEnumerable<CategoryRule> OrderByMatchPriority(IReadOnlyList<CategoryRule> rules)
    {
        return rules
            .OrderBy(rule => rule.MatchType)
            .ThenByDescending(rule => Normalize(rule.Pattern).Length);
    }

    private static bool IsMatch(
        CategoryRule rule,
        string merchantCode,
        string description)
    {
        var pattern = Normalize(rule.Pattern);

        return rule.MatchType switch
        {
            CategoryRuleMatchType.Exact => merchantCode == pattern,
            CategoryRuleMatchType.Prefix => merchantCode.StartsWith(pattern, StringComparison.Ordinal),
            CategoryRuleMatchType.Contains => description.Contains(pattern),
            _ => false
        };
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToUpperInvariant();
    }
}
