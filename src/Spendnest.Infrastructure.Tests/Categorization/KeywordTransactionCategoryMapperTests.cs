namespace Spendnest.Infrastructure.Tests.Categorization;

using FluentAssertions;
using Spendnest.Core.Categories;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Categorization;

public class KeywordTransactionCategoryMapperTests
{
    private readonly KeywordTransactionCategoryMapper mapper = new();

    [Theory]
    [InlineData("CARD MOBILE PAYMENT", BuiltInCategoryCodes.CreditCardPayment)]
    [InlineData("BULK MART #0218 RIVERTON VA", BuiltInCategoryCodes.Groceries)]
    [InlineData("CAFE RIO VERDE", BuiltInCategoryCodes.RestaurantsAndCoffee)]
    [InlineData("DOORBELL SOLO PLAN EXAMPLE.COM CA", BuiltInCategoryCodes.Subscriptions)]
    public void MapCategoryCode_ShouldMapKnownDescriptionKeywords(
        string description,
        string expectedCategoryCode)
    {
        var transaction = new Transaction
        {
            OriginalDescription = description,
            Amount = 10m
        };

        mapper.MapCategoryCode(transaction).Should().Be(expectedCategoryCode);
    }

    [Fact]
    public void MapCategoryCode_ShouldUseRefundForUnmatchedNegativeTransactions()
    {
        var transaction = new Transaction
        {
            OriginalDescription = "UNKNOWN CREDIT",
            Amount = -5m
        };

        mapper.MapCategoryCode(transaction).Should().Be(BuiltInCategoryCodes.Refund);
    }

    [Fact]
    public void MapCategoryCode_ShouldUseOtherForUnmatchedPositiveTransactions()
    {
        var transaction = new Transaction
        {
            OriginalDescription = "MYSTERY PLACE",
            Amount = 5m
        };

        mapper.MapCategoryCode(transaction).Should().Be(BuiltInCategoryCodes.Other);
    }
}
