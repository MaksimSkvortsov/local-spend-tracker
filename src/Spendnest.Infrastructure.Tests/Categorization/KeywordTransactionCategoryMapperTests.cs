namespace Spendnest.Infrastructure.Tests.Categorization;

using FluentAssertions;
using Spendnest.Core.Categories;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Categorization;

public class KeywordTransactionCategoryMapperTests
{
    private readonly KeywordTransactionCategoryMapper mapper = new();

    [Theory]
    [InlineData("CARD MOBILE PAYMENT", BuiltInCategoryIds.CreditCardPayment)]
    [InlineData("BULK MART #0218 RIVERTON VA", BuiltInCategoryIds.Groceries)]
    [InlineData("CAFE RIO VERDE", BuiltInCategoryIds.RestaurantsAndCoffee)]
    [InlineData("DOORBELL SOLO PLAN EXAMPLE.COM CA", BuiltInCategoryIds.Subscriptions)]
    public void MapCategoryId_ShouldMapKnownDescriptionKeywords(
        string description,
        int expectedCategoryId)
    {
        var transaction = new Transaction
        {
            OriginalDescription = description,
            Amount = 10m
        };

        mapper.MapCategoryId(transaction).Should().Be(expectedCategoryId);
    }

    [Fact]
    public void MapCategoryId_ShouldUseRefundForUnmatchedNegativeTransactions()
    {
        var transaction = new Transaction
        {
            OriginalDescription = "UNKNOWN CREDIT",
            Amount = -5m
        };

        mapper.MapCategoryId(transaction).Should().Be(BuiltInCategoryIds.Refund);
    }

    [Fact]
    public void MapCategoryId_ShouldKeepKnownMerchantRefundInOriginalSpendingCategory()
    {
        var transaction = new Transaction
        {
            OriginalDescription = "BULK MART REFUND RIVERTON VA",
            Amount = -14.25m
        };

        mapper.MapCategoryId(transaction).Should().Be(BuiltInCategoryIds.Groceries);
    }

    [Fact]
    public void MapCategoryId_ShouldUseOtherForUnmatchedPositiveTransactions()
    {
        var transaction = new Transaction
        {
            OriginalDescription = "MYSTERY PLACE",
            Amount = 5m
        };

        mapper.MapCategoryId(transaction).Should().Be(BuiltInCategoryIds.Other);
    }
}
