namespace Spendnest.Infrastructure.Tests.Importing;

using FluentAssertions;
using Spendnest.Core.Importing;
using Spendnest.Infrastructure.Accounts;
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
            repository,
            new InMemoryCardAccountRepository());

        var result = await service.ImportAsync(
            FixturePath("bank-of-america.csv"),
            new StatementFileImportOptions(),
            CancellationToken.None);

        var savedTransactions = await repository.ListAsync(CancellationToken.None);

        result.ParsedRowCount.Should().Be(2);
        result.SavedTransactionCount.Should().Be(2);
        result.SkippedDuplicateTransactionCount.Should().Be(0);
        result.FailedRowCount.Should().Be(0);
        result.CardAccountName.Should().Be("Default Card");
        result.Warnings.Should().BeEmpty();
        savedTransactions.Should().HaveCount(2);
        savedTransactions.Should().OnlyContain(transaction => transaction.CardAccountId == result.CardAccountId);
        savedTransactions[0].OriginalDescription.Should().Be("BULK MART #0218 RIVERTON VA");
        savedTransactions[0].Amount.Should().Be(141.83m);
    }

    [Fact]
    public async Task ImportAsync_ShouldAppendTransactionsAcrossImports()
    {
        var repository = new InMemoryTransactionRepository();
        var service = new StatementFileImportService(
            new CsvStatementParser(),
            repository,
            new InMemoryCardAccountRepository());

        await service.ImportAsync(FixturePath("bank-of-america.csv"), new StatementFileImportOptions(), CancellationToken.None);
        await service.ImportAsync(FixturePath("capital-one.csv"), new StatementFileImportOptions(), CancellationToken.None);

        var savedTransactions = await repository.ListAsync(CancellationToken.None);

        savedTransactions.Should().HaveCount(6);
        savedTransactions.Should().Contain(transaction => transaction.Amount == -2193.82m);
    }

    [Fact]
    public async Task ImportAsync_ShouldSkipTransactionsThatAlreadyExist()
    {
        var repository = new InMemoryTransactionRepository();
        var service = new StatementFileImportService(
            new CsvStatementParser(),
            repository,
            new InMemoryCardAccountRepository());

        await service.ImportAsync(FixturePath("bank-of-america.csv"), new StatementFileImportOptions(), CancellationToken.None);
        var secondResult = await service.ImportAsync(
            FixturePath("bank-of-america.csv"),
            new StatementFileImportOptions(),
            CancellationToken.None);

        var savedTransactions = await repository.ListAsync(CancellationToken.None);

        secondResult.ParsedRowCount.Should().Be(2);
        secondResult.SavedTransactionCount.Should().Be(0);
        secondResult.SkippedDuplicateTransactionCount.Should().Be(2);
        savedTransactions.Should().HaveCount(2);
    }

    [Fact]
    public async Task ImportAsync_ShouldAllowSameTransactionOnDifferentCards()
    {
        var repository = new InMemoryTransactionRepository();
        var service = new StatementFileImportService(
            new CsvStatementParser(),
            repository,
            new InMemoryCardAccountRepository());

        var firstResult = await service.ImportAsync(
            FixturePath("bank-of-america.csv"),
            new StatementFileImportOptions { CardAccountName = "Family Visa" },
            CancellationToken.None);
        var secondResult = await service.ImportAsync(
            FixturePath("bank-of-america.csv"),
            new StatementFileImportOptions { CardAccountName = "Travel Visa" },
            CancellationToken.None);

        var savedTransactions = await repository.ListAsync(CancellationToken.None);

        firstResult.SavedTransactionCount.Should().Be(2);
        secondResult.SavedTransactionCount.Should().Be(2);
        savedTransactions.Should().HaveCount(4);
        savedTransactions.Select(transaction => transaction.CardAccountId).Distinct().Should().HaveCount(2);
    }

    [Fact]
    public async Task ImportAsync_ShouldSkipDuplicateRowsInsideSameFile()
    {
        var repository = new InMemoryTransactionRepository();
        var service = new StatementFileImportService(
            new CsvStatementParser(),
            repository,
            new InMemoryCardAccountRepository());
        var filePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(
            filePath,
            """
            Posted Date,Reference Number,Payee,Address,Amount
            07/18/2026,1,"BULK MART #0218 RIVERTON VA","RIVERTON      VA ",-141.83
            07/18/2026,2,"  bulk   mart #0218 riverton va  ","RIVERTON      VA ",-141.83
            """,
            CancellationToken.None);

        try
        {
            var result = await service.ImportAsync(
                filePath,
                new StatementFileImportOptions(),
                CancellationToken.None);
            var savedTransactions = await repository.ListAsync(CancellationToken.None);

            result.ParsedRowCount.Should().Be(2);
            result.SavedTransactionCount.Should().Be(1);
            result.SkippedDuplicateTransactionCount.Should().Be(1);
            savedTransactions.Should().HaveCount(1);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static string FixturePath(string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, "Fixtures", "Csv", fileName);
    }
}
