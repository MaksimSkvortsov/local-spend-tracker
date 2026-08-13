namespace Spendnest.Application.Tests.Reporting;

using FluentAssertions;
using Spendnest.Application.Reporting;
using Spendnest.Application.Tests.TestDoubles;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Reporting;
using Spendnest.Core.Transactions;

public class CategorySpendingReportServiceTests
{
    [Fact]
    public async Task BuildAsync_ShouldGroupAllRepositoryTransactionsByCategory()
    {
        var repository = new FakeTransactionRepository();
        var assignmentRepository = new FakeTransactionCategoryAssignmentRepository();
        var reportService = new CategorySpendingReportService(
            repository,
            assignmentRepository,
            new FakeCategoryRepository(),
            new CategorySpendingReportBuilder());
        var groceries = Transaction("BULK MART #0218 RIVERTON VA", 141.83m);
        var restaurant = Transaction("CAFE RIO VERDE", 30.40m);
        var unassigned = Transaction("UNKNOWN MERCHANT", 24.13m);

        await repository.AddRangeAsync([groceries, restaurant, unassigned], CancellationToken.None);
        await assignmentRepository.SaveAsync(
            Assignment(groceries.Id, BuiltInCategoryIds.Groceries),
            CancellationToken.None);
        await assignmentRepository.SaveAsync(
            Assignment(restaurant.Id, BuiltInCategoryIds.RestaurantsAndCoffee),
            CancellationToken.None);

        var report = await reportService.BuildAsync(CancellationToken.None);

        report.Lines.Should().Contain(line =>
            line.CategoryId == BuiltInCategoryIds.Groceries
            && line.TransactionCount == 1
            && line.Amount == 141.83m);
        report.Lines.Should().Contain(line =>
            line.CategoryId == BuiltInCategoryIds.RestaurantsAndCoffee
            && line.TransactionCount == 1
            && line.Amount == 30.40m);
        report.Lines.Should().Contain(line =>
            line.CategoryId == BuiltInCategoryIds.Other
            && line.TransactionCount == 1
            && line.Amount == 24.13m);
        report.TotalSpending.Should().Be(196.36m);
    }

    [Fact]
    public async Task BuildAsync_ShouldKeepKnownMerchantRefundsInsideOriginalCategory()
    {
        var repository = new FakeTransactionRepository();
        var assignmentRepository = new FakeTransactionCategoryAssignmentRepository();
        var reportService = new CategorySpendingReportService(
            repository,
            assignmentRepository,
            new FakeCategoryRepository(),
            new CategorySpendingReportBuilder());
        var purchase = Transaction("BULK MART #0218 RIVERTON VA", 141.83m);
        var refund = Transaction("BULK MART REFUND RIVERTON VA", -14.25m);
        await repository.AddRangeAsync(
            [
                purchase,
                refund
            ],
            CancellationToken.None);
        await assignmentRepository.SaveAsync(
            Assignment(purchase.Id, BuiltInCategoryIds.Groceries),
            CancellationToken.None);
        await assignmentRepository.SaveAsync(
            Assignment(refund.Id, BuiltInCategoryIds.Groceries),
            CancellationToken.None);

        var report = await reportService.BuildAsync(CancellationToken.None);

        report.Lines.Should().ContainSingle(line => line.CategoryId == BuiltInCategoryIds.Groceries)
            .Which.Should().BeEquivalentTo(new
            {
                CategoryId = BuiltInCategoryIds.Groceries,
                CategoryName = "Groceries",
                TransactionCount = 2,
                Amount = 127.58m
            });
        report.Lines.Should().NotContain(line => line.CategoryName == "Refund");
        report.TotalSpending.Should().Be(127.58m);
    }

    [Fact]
    public async Task BuildAsync_ShouldExcludeCreditCardPaymentsFromSpending()
    {
        var repository = new FakeTransactionRepository();
        var assignmentRepository = new FakeTransactionCategoryAssignmentRepository();
        var reportService = new CategorySpendingReportService(
            repository,
            assignmentRepository,
            new FakeCategoryRepository(),
            new CategorySpendingReportBuilder());
        var groceries = Transaction("BULK MART #0218 RIVERTON VA", 141.83m);
        var travel = Transaction("UNITED AIRLINES", 750m);
        var payment = Transaction("CAPITAL ONE MOBILE PYMT", -700m);
        await repository.AddRangeAsync(
            [
                groceries,
                travel,
                payment
            ],
            CancellationToken.None);
        await assignmentRepository.SaveAsync(
            Assignment(groceries.Id, BuiltInCategoryIds.Groceries),
            CancellationToken.None);
        await assignmentRepository.SaveAsync(
            Assignment(travel.Id, BuiltInCategoryIds.Travel),
            CancellationToken.None);
        await assignmentRepository.SaveAsync(
            Assignment(payment.Id, BuiltInCategoryIds.CreditCardPayment),
            CancellationToken.None);

        var report = await reportService.BuildAsync(CancellationToken.None);

        report.Lines.Should().NotContain(line => line.CategoryId == BuiltInCategoryIds.CreditCardPayment);
        report.TotalSpending.Should().Be(891.83m);
    }

    [Fact]
    public async Task BuildAsync_ShouldOrderLinesByAbsoluteAmountThenCategoryNameAndFallbackMissingCategoryNames()
    {
        var repository = new FakeTransactionRepository();
        var assignmentRepository = new FakeTransactionCategoryAssignmentRepository();
        var reportService = new CategorySpendingReportService(
            repository,
            assignmentRepository,
            new FakeCategoryRepository(),
            new CategorySpendingReportBuilder());
        var unknownCategory = Transaction("UNKNOWN CATEGORY MERCHANT", -50m);
        var groceries = Transaction("BULK MART #0218 RIVERTON VA", 20m);
        var restaurant = Transaction("CAFE RIO VERDE", 20m);
        await repository.AddRangeAsync(
            [
                restaurant,
                groceries,
                unknownCategory
            ],
            CancellationToken.None);
        await assignmentRepository.SaveAsync(
            Assignment(unknownCategory.Id, 9999),
            CancellationToken.None);
        await assignmentRepository.SaveAsync(
            Assignment(groceries.Id, BuiltInCategoryIds.Groceries),
            CancellationToken.None);
        await assignmentRepository.SaveAsync(
            Assignment(restaurant.Id, BuiltInCategoryIds.RestaurantsAndCoffee),
            CancellationToken.None);

        var report = await reportService.BuildAsync(CancellationToken.None);

        report.Lines.Should().Equal(
            new CategorySpendingReportLine(9999, "9999", 1, -50m),
            new CategorySpendingReportLine(BuiltInCategoryIds.Groceries, "Groceries", 1, 20m),
            new CategorySpendingReportLine(BuiltInCategoryIds.RestaurantsAndCoffee, "Restaurants & Coffee", 1, 20m));
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
