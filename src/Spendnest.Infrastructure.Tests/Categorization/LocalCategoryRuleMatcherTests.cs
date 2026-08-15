namespace Spendnest.Infrastructure.Tests.Categorization;

using FluentAssertions;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Categorization;

public class LocalCategoryRuleMatcherTests
{
    [Fact]
    public void FindMatch_ShouldPreferExactRulesBeforePrefixAndContainsRules()
    {
        var matcher = CreateMatcher();
        var transaction = Transaction("AMAZON PRIME MONTHLY");
        var exactRule = Rule("AMAZON PRIME MONTHLY", BuiltInCategoryIds.Subscriptions, CategoryRuleMatchType.Exact);
        var prefixRule = Rule("AMAZON PRIME", BuiltInCategoryIds.Shopping, CategoryRuleMatchType.Prefix);
        var containsRule = Rule("AMAZON", BuiltInCategoryIds.Other, CategoryRuleMatchType.Contains);

        var result = matcher.FindMatch(
            transaction,
            [
                containsRule,
                prefixRule,
                exactRule
            ]);

        result.Should().BeSameAs(exactRule);
    }

    [Fact]
    public void FindMatch_ShouldUseLongestRuleWhenMatchTypeIsTheSame()
    {
        var matcher = CreateMatcher();
        var transaction = Transaction("AMAZON PRIME MONTHLY");
        var shorterRule = Rule("AMAZON", BuiltInCategoryIds.Shopping, CategoryRuleMatchType.Prefix);
        var longerRule = Rule("AMAZON PRIME", BuiltInCategoryIds.Subscriptions, CategoryRuleMatchType.Prefix);

        var result = matcher.FindMatch(
            transaction,
            [
                shorterRule,
                longerRule
            ]);

        result.Should().BeSameAs(longerRule);
    }

    [Fact]
    public void FindMatch_ShouldMatchExactRulesAgainstResolvedMerchantCode()
    {
        var matcher = CreateMatcher();
        var transaction = Transaction("PAYPAL *NETFLIX 1234");
        var exactRule = Rule("PAYPAL", BuiltInCategoryIds.Subscriptions, CategoryRuleMatchType.Exact);
        var rawDescriptionContainsRule = Rule("NETFLIX", BuiltInCategoryIds.Entertainment, CategoryRuleMatchType.Contains);

        var result = matcher.FindMatch(
            transaction,
            [
                rawDescriptionContainsRule,
                exactRule
            ]);

        result.Should().BeSameAs(exactRule);
    }

    [Fact]
    public void FindMatch_ShouldMatchPrefixRulesAgainstResolvedMerchantCode()
    {
        var matcher = CreateMatcher();
        var transaction = Transaction("PAYPAL *NETFLIX 1234");
        var prefixRule = Rule("PAY", BuiltInCategoryIds.Subscriptions, CategoryRuleMatchType.Prefix);

        var result = matcher.FindMatch(transaction, [prefixRule]);

        result.Should().BeSameAs(prefixRule);
    }

    [Fact]
    public void FindMatch_ShouldMatchContainsRulesAgainstOriginalDescription()
    {
        var matcher = CreateMatcher();
        var transaction = Transaction("CORNER MARKET REFUND");
        var rule = Rule("corner market", BuiltInCategoryIds.Groceries, CategoryRuleMatchType.Contains);

        var result = matcher.FindMatch(transaction, [rule]);

        result.Should().BeSameAs(rule);
    }

    [Fact]
    public void FindMatch_ShouldReturnNullWhenNoRuleMatches()
    {
        var matcher = CreateMatcher();
        var transaction = Transaction("UNKNOWN CREDIT");
        var rule = Rule("CORNER MARKET", BuiltInCategoryIds.Groceries, CategoryRuleMatchType.Contains);

        var result = matcher.FindMatch(transaction, [rule]);

        result.Should().BeNull();
    }

    private static LocalCategoryRuleMatcher CreateMatcher()
    {
        return new LocalCategoryRuleMatcher(new TransactionMerchantCodeResolver());
    }

    private static CategoryRule Rule(
        string pattern,
        int categoryId,
        CategoryRuleMatchType matchType)
    {
        return new CategoryRule
        {
            Pattern = pattern,
            CategoryId = categoryId,
            MatchType = matchType
        };
    }

    private static Transaction Transaction(string description)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            OriginalDescription = description,
            Amount = 10m
        };
    }
}
