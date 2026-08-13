namespace Spendnest.Application.Tests.Reporting;

using FluentAssertions;
using Spendnest.Application.Reporting;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Reporting;
using Spendnest.Core.Transactions;

public class CategorySpendingReportBuilderTests
{
    [Fact]
    public void Build_ShouldOrderLinesByAbsoluteAmountThenCategoryNameAndFallbackMissingCategoryNames()
    {
        var builder = new CategorySpendingReportBuilder();
        var unknownCategory = Transaction("UNKNOWN CATEGORY MERCHANT", -50m);
        var groceries = Transaction("BULK MART #0218 RIVERTON VA", 20m);
        var restaurant = Transaction("CAFE RIO VERDE", 20m);

        var report = builder.Build(
            [restaurant, groceries, unknownCategory],
            [
                Assignment(unknownCategory.Id, 9999),
                Assignment(groceries.Id, BuiltInCategoryIds.Groceries),
                Assignment(restaurant.Id, BuiltInCategoryIds.RestaurantsAndCoffee)
            ],
            BuiltInCategories.All);

        report.Lines.Should().Equal(
            new CategorySpendingReportLine(9999, "9999", 1, -50m),
            new CategorySpendingReportLine(BuiltInCategoryIds.Groceries, "Groceries", 1, 20m),
            new CategorySpendingReportLine(BuiltInCategoryIds.RestaurantsAndCoffee, "Restaurants & Coffee", 1, 20m));
    }

    [Fact]
    public void Build_ShouldExcludeCreditCardPaymentsFromSpending()
    {
        var builder = new CategorySpendingReportBuilder();
        var groceries = Transaction("BULK MART #0218 RIVERTON VA", 141.83m);
        var payment = Transaction("CAPITAL ONE MOBILE PYMT", -700m);

        var report = builder.Build(
            [groceries, payment],
            [
                Assignment(groceries.Id, BuiltInCategoryIds.Groceries),
                Assignment(payment.Id, BuiltInCategoryIds.CreditCardPayment)
            ],
            BuiltInCategories.All);

        report.Lines.Should().ContainSingle().Which.Should().Be(
            new CategorySpendingReportLine(BuiltInCategoryIds.Groceries, "Groceries", 1, 141.83m));
        report.TotalSpending.Should().Be(141.83m);
    }

    [Fact]
    public void Build_ShouldUseOtherForUnassignedTransactions()
    {
        var builder = new CategorySpendingReportBuilder();
        var transaction = Transaction("UNKNOWN MERCHANT", 24.13m);

        var report = builder.Build(
            [transaction],
            [],
            BuiltInCategories.All);

        report.Lines.Should().ContainSingle().Which.Should().Be(
            new CategorySpendingReportLine(BuiltInCategoryIds.Other, "Other", 1, 24.13m));
        report.TotalSpending.Should().Be(24.13m);
    }

    private static Transaction Transaction(
        string description,
        decimal amount)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            CardAccountId = Guid.NewGuid(),
            PostedDate = new DateOnly(2026, 7, 18),
            OriginalDescription = description,
            Amount = amount,
            ImportedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static TransactionCategoryAssignment Assignment(
        Guid transactionId,
        int categoryId)
    {
        return new TransactionCategoryAssignment
        {
            TransactionId = transactionId,
            CategoryId = categoryId,
            Confidence = 1m,
            NeedsReview = false,
            Source = CategorizationSource.LocalRules,
            Explanation = "Matched learned merchant rule."
        };
    }
}
