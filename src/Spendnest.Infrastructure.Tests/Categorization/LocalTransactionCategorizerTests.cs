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
            new KeywordTransactionCategoryMapper());
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
            new KeywordTransactionCategoryMapper());
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OriginalDescription = "UNKNOWN CREDIT",
            Amount = -12.30m
        };

        var result = await categorizer.CategorizeKnownAsync([transaction], CancellationToken.None);

        result.Should().BeEmpty();
    }
}
