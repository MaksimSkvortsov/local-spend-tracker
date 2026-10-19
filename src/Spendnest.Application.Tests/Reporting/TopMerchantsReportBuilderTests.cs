using FluentAssertions;
using Spendnest.Application.Categorization;
using Spendnest.Application.Reporting;
using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Categorization;

namespace Spendnest.Application.Tests.Reporting;

public class TopMerchantsReportBuilderTests
{
    private readonly TransactionMerchantCodeResolver resolver = new();

    [Fact]
    public void Build_ShouldCombineDifferentMerchantCodesUsingTheWinningRule()
    {
        var lines = Build(
            [Transaction("AMAZON MARKETPLACE", 40), Transaction("AMAZON RETAIL", 60)],
            [Rule(" amazon ", CategoryRuleMatchType.Prefix)]);

        lines.Should().Equal(new TopMerchantLine("AMAZON", 2, 100));
    }

    [Fact]
    public void Build_ShouldPreserveExactAndLongestPrefixPriorityOverContains()
    {
        var lines = Build(
            [Transaction("AMAZON PRIME", 30), Transaction("AMAZON RETAIL SHOP", 20), Transaction("AMAZON MARKETPLACE", 10)],
            [Rule("AMAZON", CategoryRuleMatchType.Contains), Rule("AMAZON", CategoryRuleMatchType.Prefix),
             Rule("AMAZON RETAIL", CategoryRuleMatchType.Prefix), Rule("AMAZON PRIME", CategoryRuleMatchType.Exact)]);

        lines.Should().Equal(
            new TopMerchantLine("AMAZON PRIME", 1, 30),
            new TopMerchantLine("AMAZON RETAIL", 1, 20),
            new TopMerchantLine("AMAZON", 1, 10));
    }

    [Fact]
    public void Build_ShouldMatchContainsAgainstRawDescriptionAndKeepDifferentRulesSeparate()
    {
        var lines = Build(
            [Transaction("PAYPAL *NETFLIX", 30), Transaction("DIRECT NETFLIX", 20), Transaction("NETFLIX", 10)],
            [Rule("NETFLIX", CategoryRuleMatchType.Contains), Rule("NETFLIX", CategoryRuleMatchType.Exact)]);

        lines.Should().Equal(new TopMerchantLine("NETFLIX", 2, 50), new TopMerchantLine("NETFLIX", 1, 10));
    }

    [Fact]
    public void Build_ShouldFallBackToMerchantCodesWithoutRules()
    {
        var lines = Build(
            [Transaction("CAFE #123", 10), Transaction("CAFE #456", 20), Transaction(" ", 5)], []);

        lines.Should().Equal(new TopMerchantLine("CAFE", 2, 30), new TopMerchantLine("Unknown Merchant", 1, 5));
    }

    [Fact]
    public void Build_ShouldSubtractRefundsBeforeRankingAndExcludeNonpositiveTotals()
    {
        var lines = Build(
            [Transaction("AMAZON RETAIL", 100), Transaction("AMAZON MARKETPLACE REFUND", -90),
             Transaction("CAFE", 30), Transaction("SHOP", 20), Transaction("SHOP REFUND", -20),
             Transaction("REFUND ONLY", -10)],
            [Rule("AMAZON", CategoryRuleMatchType.Prefix)]);

        lines.Should().Equal(new TopMerchantLine("CAFE", 1, 30), new TopMerchantLine("AMAZON", 2, 10));
    }

    [Fact]
    public void Build_ShouldReturnAtMostFiveMerchantsWithStableOrderingForTies()
    {
        var lines = Build(
            [Transaction("F", 10), Transaction("E", 10), Transaction("D", 10),
             Transaction("C", 10), Transaction("B", 10), Transaction("A", 10)], []);

        lines.Select(line => line.DisplayName).Should().Equal("A", "B", "C", "D", "E");
        Build([], []).Should().BeEmpty();
    }

    private IReadOnlyList<TopMerchantLine> Build(IReadOnlyList<Transaction> transactions, IReadOnlyList<CategoryRule> rules)
        => new TopMerchantsReportBuilder(new LocalCategoryRuleMatcher(resolver), resolver).Build(transactions, rules);

    private static Transaction Transaction(string description, decimal amount)
        => new() { OriginalDescription = description, Amount = amount };

    private static CategoryRule Rule(string pattern, CategoryRuleMatchType matchType)
        => new() { Pattern = pattern, MatchType = matchType };
}
