using Spendnest.Application.Categorization;
using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;

namespace Spendnest.Application.Reporting;

public sealed class TopMerchantsReportBuilder(
    LocalCategoryRuleMatcher ruleMatcher,
    ITransactionMerchantCodeResolver merchantCodeResolver)
{
    // The caller supplies spending transactions for the selected report window.
    public IReadOnlyList<TopMerchantLine> Build(
        IReadOnlyList<Transaction> transactions,
        IReadOnlyList<CategoryRule> rules)
    {
        return transactions
            .GroupBy(transaction => ResolveGroup(transaction, rules))
            .Select(group => new TopMerchantLine(
                group.Key.DisplayName,
                group.Count(),
                group.Sum(transaction => transaction.Amount)))
            .Where(line => line.Amount > 0)
            .OrderByDescending(line => line.Amount)
            .ThenBy(line => line.DisplayName, StringComparer.Ordinal)
            .Take(5)
            .ToArray();
    }

    private MerchantGroup ResolveGroup(Transaction transaction, IReadOnlyList<CategoryRule> rules)
    {
        var rule = ruleMatcher.FindMatch(transaction, rules);
        if (rule is not null)
        {
            return new MerchantGroup(rule.Id, rule.Pattern.Trim().ToUpperInvariant());
        }

        var merchantCode = merchantCodeResolver.Resolve(transaction);
        return new MerchantGroup(null, string.IsNullOrWhiteSpace(merchantCode) ? "Unknown Merchant" : merchantCode);
    }

    private sealed record MerchantGroup(Guid? RuleId, string DisplayName);
}
