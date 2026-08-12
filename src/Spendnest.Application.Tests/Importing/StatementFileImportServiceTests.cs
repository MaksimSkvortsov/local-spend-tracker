namespace Spendnest.Application.Tests.Importing;

using FluentAssertions;
using Spendnest.Application.Importing;
using Spendnest.Core.Importing;
using Spendnest.Core.Progress;
using Spendnest.Infrastructure.Accounts;
using Spendnest.Infrastructure.Importing;
using Spendnest.Infrastructure.Transactions;

public class StatementFileImportServiceTests
{
    [Fact]
    public async Task ImportAsync_ShouldParseAndSaveTransactionsToRepository()
    {
        var repository = new InMemoryTransactionRepository();
        var service = CreateService(repository);

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
    public async Task ImportAsync_ShouldRecordStatementImportHistory()
    {
        var repository = new InMemoryTransactionRepository();
        var statementImportRepository = new InMemoryStatementImportRepository();
        var service = CreateService(repository, statementImportRepository);

        var filePath = FixturePath("bank-of-america.csv");
        var result = await service.ImportAsync(
            filePath,
            new StatementFileImportOptions(),
            CancellationToken.None);

        var statementImports = await statementImportRepository.ListAsync(CancellationToken.None);
        var savedTransactions = await repository.ListAsync(CancellationToken.None);

        var statementImport = statementImports.Should().ContainSingle().Subject;
        statementImport.Id.Should().Be(result.StatementImportId);
        statementImport.CardAccountId.Should().Be(result.CardAccountId);
        statementImport.FilePath.Should().Be(filePath);
        statementImport.FileName.Should().Be("bank-of-america.csv");
        statementImport.FileHash.Should().NotBeNullOrWhiteSpace();
        statementImport.Status.Should().Be(StatementImportStatus.Completed);
        statementImport.ParsedRowCount.Should().Be(2);
        statementImport.SavedTransactionCount.Should().Be(2);
        statementImport.SkippedDuplicateTransactionCount.Should().Be(0);
        statementImport.FailedRowCount.Should().Be(0);
        statementImport.CompletedAtUtc.Should().NotBeNull();
        savedTransactions.Should().OnlyContain(transaction => transaction.StatementImportId == statementImport.Id);
    }

    [Fact]
    public async Task ImportAsync_ShouldReportLongRunningProgressStages()
    {
        var repository = new InMemoryTransactionRepository();
        var service = CreateService(repository);
        var progress = new RecordingProgress();

        await service.ImportAsync(
            FixturePath("bank-of-america.csv"),
            new StatementFileImportOptions { Progress = progress },
            CancellationToken.None);

        progress.Events.Select(progressEvent => progressEvent.Stage)
            .Should().Equal(
                FileUploadProgressStage.ReadingFile,
                FileUploadProgressStage.ParsingTransactions,
                FileUploadProgressStage.SavingTransactions);
        progress.Events.Should().ContainSingle(progressEvent =>
            progressEvent.Stage == FileUploadProgressStage.SavingTransactions
            && progressEvent.Current == 2
            && progressEvent.Total == 2);
    }

    [Fact]
    public async Task ImportAsync_ShouldMarkStatementImportFailedWhenParsingFails()
    {
        var repository = new InMemoryTransactionRepository();
        var statementImportRepository = new InMemoryStatementImportRepository();
        var service = CreateService(
            repository,
            statementImportRepository,
            new ThrowingStatementParser());

        var act = async () => await service.ImportAsync(
            FixturePath("bank-of-america.csv"),
            new StatementFileImportOptions(),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        var statementImport = (await statementImportRepository.ListAsync(CancellationToken.None))
            .Should()
            .ContainSingle()
            .Subject;
        statementImport.Status.Should().Be(StatementImportStatus.Failed);
        statementImport.ErrorMessage.Should().Be("Parser failed.");
        statementImport.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ImportAsync_ShouldAppendTransactionsAcrossImports()
    {
        var repository = new InMemoryTransactionRepository();
        var service = CreateService(repository);

        await service.ImportAsync(FixturePath("bank-of-america.csv"), new StatementFileImportOptions(), CancellationToken.None);
        await service.ImportAsync(FixturePath("capital-one.csv"), new StatementFileImportOptions(), CancellationToken.None);

        var savedTransactions = await repository.ListAsync(CancellationToken.None);

        savedTransactions.Should().HaveCount(6);
        savedTransactions.Should().Contain(transaction => transaction.Amount == -2193.82m);
    }

    [Fact]
    public async Task ImportAsync_ShouldRejectStatementFileThatWasAlreadyImported()
    {
        var repository = new InMemoryTransactionRepository();
        var statementImportRepository = new InMemoryStatementImportRepository();
        var service = CreateService(repository, statementImportRepository);

        await service.ImportAsync(FixturePath("bank-of-america.csv"), new StatementFileImportOptions(), CancellationToken.None);
        var act = async () => await service.ImportAsync(
            FixturePath("bank-of-america.csv"),
            new StatementFileImportOptions(),
            CancellationToken.None);

        var savedTransactions = await repository.ListAsync(CancellationToken.None);
        var statementImports = await statementImportRepository.ListAsync(CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateStatementImportException>()
            .WithMessage("'bank-of-america.csv' has already been imported.");
        savedTransactions.Should().HaveCount(2);
        statementImports.Should().ContainSingle();
    }

    [Fact]
    public async Task ImportAsync_ShouldAllowSameTransactionOnDifferentCards()
    {
        var repository = new InMemoryTransactionRepository();
        var service = CreateService(repository);
        var firstFilePath = Path.GetTempFileName();
        var secondFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(
            firstFilePath,
            """
            Posted Date,Reference Number,Payee,Address,Amount
            07/18/2026,1,"BULK MART #0218 RIVERTON VA","RIVERTON      VA ",-141.83
            """,
            CancellationToken.None);
        await File.WriteAllTextAsync(
            secondFilePath,
            """
            Posted Date,Reference Number,Payee,Address,Amount
            07/18/2026,2,"BULK MART #0218 RIVERTON VA","RIVERTON      VA ",-141.83
            """,
            CancellationToken.None);

        try
        {
            var firstResult = await service.ImportAsync(
                firstFilePath,
                new StatementFileImportOptions { CardAccountName = "Family Visa" },
                CancellationToken.None);
            var secondResult = await service.ImportAsync(
                secondFilePath,
                new StatementFileImportOptions { CardAccountName = "Travel Visa" },
                CancellationToken.None);

            var savedTransactions = await repository.ListAsync(CancellationToken.None);

            firstResult.SavedTransactionCount.Should().Be(1);
            secondResult.SavedTransactionCount.Should().Be(1);
            savedTransactions.Should().HaveCount(2);
            savedTransactions.Select(transaction => transaction.CardAccountId).Distinct().Should().HaveCount(2);
        }
        finally
        {
            File.Delete(firstFilePath);
            File.Delete(secondFilePath);
        }
    }

    [Fact]
    public async Task ImportAsync_ShouldSkipSameTransactionFromDifferentStatementFiles()
    {
        var repository = new InMemoryTransactionRepository();
        var service = CreateService(repository);
        var firstFilePath = Path.GetTempFileName();
        var secondFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(
            firstFilePath,
            """
            Posted Date,Reference Number,Payee,Address,Amount
            07/18/2026,1,"BULK MART #0218 RIVERTON VA","RIVERTON      VA ",-141.83
            """,
            CancellationToken.None);
        await File.WriteAllTextAsync(
            secondFilePath,
            """
            Posted Date,Reference Number,Payee,Address,Amount
            07/18/2026,2,"BULK MART #0218 RIVERTON VA","RIVERTON      VA ",-141.83
            """,
            CancellationToken.None);

        try
        {
            await service.ImportAsync(firstFilePath, new StatementFileImportOptions(), CancellationToken.None);
            var secondResult = await service.ImportAsync(secondFilePath, new StatementFileImportOptions(), CancellationToken.None);

            var savedTransactions = await repository.ListAsync(CancellationToken.None);

            secondResult.ParsedRowCount.Should().Be(1);
            secondResult.SavedTransactionCount.Should().Be(0);
            secondResult.SkippedDuplicateTransactionCount.Should().Be(1);
            savedTransactions.Should().HaveCount(1);
        }
        finally
        {
            File.Delete(firstFilePath);
            File.Delete(secondFilePath);
        }
    }

    [Fact]
    public async Task ImportAsync_ShouldSkipDuplicateRowsInsideSameFile()
    {
        var repository = new InMemoryTransactionRepository();
        var service = CreateService(repository);
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

    private static StatementFileImportService CreateService(
        InMemoryTransactionRepository repository,
        InMemoryStatementImportRepository? statementImportRepository = null,
        IStatementParser? parser = null)
    {
        return new StatementFileImportService(
            parser ?? new CsvStatementParser(),
            repository,
            new InMemoryCardAccountRepository(),
            statementImportRepository ?? new InMemoryStatementImportRepository(),
            new LocalStatementFileReader());
    }

    private sealed class RecordingProgress : IProgress<FileUploadProgress>
    {
        private readonly List<FileUploadProgress> events = [];

        public IReadOnlyList<FileUploadProgress> Events => events;

        public void Report(FileUploadProgress value)
        {
            events.Add(value);
        }
    }

    private sealed class ThrowingStatementParser : IStatementParser
    {
        public Task<StatementParseResult> ParseAsync(
            Stream stream,
            StatementParseOptions options,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Parser failed.");
        }
    }
}
