namespace Spendnest.Infrastructure.Tests.Importing;

using FluentAssertions;
using Spendnest.Core.Importing;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Importing;
using Spendnest.Infrastructure.Transactions;

public class StatementFileImportServiceTests
{
    [Fact]
    public async Task ImportAsync_ShouldParseAndSaveTransactionsToRepository()
    {
        var repository = new InMemoryTransactionRepository();
        var service = new StatementFileImportService(
            new CsvStatementParser(),
            repository);

        var result = await service.ImportAsync(
            FixturePath("bank-of-america.csv"),
            CancellationToken.None);

        var savedTransactions = await repository.ListAsync(CancellationToken.None);

        result.ParsedRowCount.Should().Be(2);
        result.SavedTransactionCount.Should().Be(2);
        result.FailedRowCount.Should().Be(0);
        result.Warnings.Should().BeEmpty();
        savedTransactions.Should().HaveCount(2);
        savedTransactions[0].OriginalDescription.Should().Be("BULK MART #0218 RIVERTON VA");
        savedTransactions[0].Amount.Should().Be(141.83m);
    }

    [Fact]
    public async Task ImportAsync_ShouldAppendTransactionsAcrossImports()
    {
        var repository = new InMemoryTransactionRepository();
        var service = new StatementFileImportService(
            new CsvStatementParser(),
            repository);

        await service.ImportAsync(FixturePath("bank-of-america.csv"), CancellationToken.None);
        await service.ImportAsync(FixturePath("capital-one.csv"), CancellationToken.None);

        var savedTransactions = await repository.ListAsync(CancellationToken.None);

        savedTransactions.Should().HaveCount(6);
        savedTransactions.Should().Contain(transaction => transaction.Amount == -2193.82m);
    }

    private static string FixturePath(string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, "Fixtures", "Csv", fileName);
    }
}
