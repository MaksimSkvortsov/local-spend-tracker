namespace Spendnest.Infrastructure.Tests.Reporting;

using FluentAssertions;
using Spendnest.Core.Categories;
using Spendnest.Core.Importing;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Accounts;
using Spendnest.Infrastructure.Categorization;
using Spendnest.Infrastructure.Importing;
using Spendnest.Infrastructure.Reporting;
using Spendnest.Infrastructure.Transactions;

public class CategorySpendingReportServiceTests
{
    [Fact]
    public async Task BuildAsync_ShouldGroupAllRepositoryTransactionsByCategory()
    {
        var repository = new InMemoryTransactionRepository();
        var importService = new StatementFileImportService(
            new CsvStatementParser(),
            repository,
            new InMemoryCardAccountRepository());
        var reportService = new CategorySpendingReportService(
            repository,
            new InMemoryTransactionCategoryAssignmentRepository(),
            new KeywordTransactionCategoryMapper());

        await importService.ImportAsync(FixturePath("bank-of-america.csv"), new StatementFileImportOptions(), CancellationToken.None);
        await importService.ImportAsync(FixturePath("capital-one.csv"), new StatementFileImportOptions(), CancellationToken.None);

        var report = await reportService.BuildAsync(CancellationToken.None);

        report.Lines.Should().Contain(line =>
            line.CategoryId == BuiltInCategoryIds.Groceries
            && line.TransactionCount == 2
            && line.Amount == 165.96m);
        report.Lines.Should().Contain(line =>
            line.CategoryId == BuiltInCategoryIds.RestaurantsAndCoffee
            && line.TransactionCount == 2
            && line.Amount == 30.40m);
        report.Lines.Should().Contain(line =>
            line.CategoryId == BuiltInCategoryIds.Subscriptions
            && line.TransactionCount == 1
            && line.Amount == 4.99m);
        report.Lines.Should().Contain(line =>
            line.CategoryId == BuiltInCategoryIds.CreditCardPayment
            && line.TransactionCount == 1
            && line.Amount == -2193.82m);
        report.TotalSpending.Should().Be(-1992.47m);
    }

    [Fact]
    public async Task BuildAsync_ShouldKeepKnownMerchantRefundsInsideOriginalCategory()
    {
        var repository = new InMemoryTransactionRepository();
        var reportService = new CategorySpendingReportService(
            repository,
            new InMemoryTransactionCategoryAssignmentRepository(),
            new KeywordTransactionCategoryMapper());
        await repository.AddRangeAsync(
            [
                Transaction("BULK MART #0218 RIVERTON VA", 141.83m),
                Transaction("BULK MART REFUND RIVERTON VA", -14.25m)
            ],
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

    private static string FixturePath(string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, "Fixtures", "Csv", fileName);
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
}
