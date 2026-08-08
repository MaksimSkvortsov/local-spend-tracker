namespace Spendnest.Infrastructure.Tests.Categorization;

using FluentAssertions;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Categorization;

public class LocalTransactionCategorizerTests
{
    [Fact]
    public async Task CategorizeKnownAsync_ShouldUseStoredLocalRules()
    {
        var ruleRepository = new InMemoryCategoryRuleRepository();
        await ruleRepository.AddAsync(new CategoryRule
        {
            Pattern = "CORNER MARKET",
            CategoryId = BuiltInCategoryIds.Groceries,
            MatchType = CategoryRuleMatchType.Contains
        }, CancellationToken.None);
        var categorizer = new LocalTransactionCategorizer(
            ruleRepository,
            new TransactionMerchantCodeResolver());
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OriginalDescription = "CORNER MARKET REFUND",
            Amount = -12.30m
        };

        var result = await categorizer.CategorizeKnownAsync([transaction], CancellationToken.None);

        result.Should().ContainSingle().Which.Should().BeEquivalentTo(new TransactionCategorization(
            transaction.Id,
            BuiltInCategoryIds.Groceries,
            1m,
            false,
            CategorizationSource.LocalRules,
            "Matched local categorization knowledge."));
    }

    [Fact]
    public async Task CategorizeKnownAsync_ShouldLeaveUnmatchedRefundsForAi()
    {
        var categorizer = new LocalTransactionCategorizer(
            new InMemoryCategoryRuleRepository(),
            new TransactionMerchantCodeResolver());
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OriginalDescription = "UNKNOWN CREDIT",
            Amount = -12.30m
        };

        var result = await categorizer.CategorizeKnownAsync([transaction], CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CategorizeKnownAsync_ShouldUseLongestStoredPrefixRule()
    {
        var ruleRepository = new InMemoryCategoryRuleRepository();
        await ruleRepository.AddAsync(new CategoryRule
        {
            Pattern = "AMAZON",
            CategoryId = BuiltInCategoryIds.Shopping,
            MatchType = CategoryRuleMatchType.Prefix
        }, CancellationToken.None);
        await ruleRepository.AddAsync(new CategoryRule
        {
            Pattern = "AMAZON PRIME",
            CategoryId = BuiltInCategoryIds.Subscriptions,
            MatchType = CategoryRuleMatchType.Prefix
        }, CancellationToken.None);
        var categorizer = new LocalTransactionCategorizer(
            ruleRepository,
            new TransactionMerchantCodeResolver());
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OriginalDescription = "AMAZON PRIME MONTHLY",
            Amount = 14.99m
        };

        var result = await categorizer.CategorizeKnownAsync([transaction], CancellationToken.None);

        result.Should().ContainSingle(categorization =>
            categorization.TransactionId == transaction.Id
            && categorization.CategoryId == BuiltInCategoryIds.Subscriptions);
    }
}
