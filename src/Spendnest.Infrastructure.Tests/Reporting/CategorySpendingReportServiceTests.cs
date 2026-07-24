namespace Spendnest.Infrastructure.Tests.Reporting;

using FluentAssertions;
using Spendnest.Core.Categories;
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
            repository);
        var reportService = new CategorySpendingReportService(
            repository,
            new KeywordTransactionCategoryMapper());

        await importService.ImportAsync(FixturePath("bank-of-america.csv"), CancellationToken.None);
        await importService.ImportAsync(FixturePath("capital-one.csv"), CancellationToken.None);

        var report = await reportService.BuildAsync(CancellationToken.None);

        report.Lines.Should().Contain(line =>
            line.CategoryCode == BuiltInCategoryCodes.Groceries
            && line.TransactionCount == 2
            && line.Amount == 165.96m);
        report.Lines.Should().Contain(line =>
            line.CategoryCode == BuiltInCategoryCodes.RestaurantsAndCoffee
            && line.TransactionCount == 2
            && line.Amount == 30.40m);
        report.Lines.Should().Contain(line =>
            line.CategoryCode == BuiltInCategoryCodes.Subscriptions
            && line.TransactionCount == 1
            && line.Amount == 4.99m);
        report.Lines.Should().Contain(line =>
            line.CategoryCode == BuiltInCategoryCodes.CreditCardPayment
            && line.TransactionCount == 1
            && line.Amount == -2193.82m);
        report.TotalSpending.Should().Be(-1992.47m);
    }

    private static string FixturePath(string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, "Fixtures", "Csv", fileName);
    }
}
