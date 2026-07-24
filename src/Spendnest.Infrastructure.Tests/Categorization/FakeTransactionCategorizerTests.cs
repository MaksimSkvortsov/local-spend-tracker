namespace Spendnest.Infrastructure.Tests.Categorization;

using FluentAssertions;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Categorization;

public class FakeTransactionCategorizerTests
{
    [Fact]
    public async Task CategorizeAsync_ShouldUseDeterministicKeywordMapper()
    {
        var categorizer = new FakeTransactionCategorizer(new KeywordTransactionCategoryMapper());
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OriginalDescription = "CAFE RIO VERDE",
            Amount = 8.40m
        };

        var result = await categorizer.CategorizeAsync([transaction], CancellationToken.None);

        result.Should().ContainSingle().Which.Should().BeEquivalentTo(new TransactionCategorization(
            transaction.Id,
            BuiltInCategoryCodes.RestaurantsAndCoffee,
            1m,
            false,
            CategorizationSource.FakeAi,
            "Deterministic offline categorizer."));
    }
}
