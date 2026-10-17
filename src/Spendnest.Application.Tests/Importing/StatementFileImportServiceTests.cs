namespace Spendnest.Application.Tests.Importing;

using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Spendnest.Application.Importing;
using Spendnest.Application.Tests.TestDoubles;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Importing;
using Spendnest.Core.Progress;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Importing;

public class StatementFileImportServiceTests
{
    [Fact]
    public async Task ImportAsync_ShouldParseAndSaveTransactionsToRepository()
    {
        var repository = new FakeTransactionRepository();
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
        var repository = new FakeTransactionRepository();
        var statementImportRepository = new FakeStatementImportRepository();
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
        var repository = new FakeTransactionRepository();
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
                FileUploadProgressStage.CategorizingWithAi,
                FileUploadProgressStage.SavingTransactions);
        progress.Events.Should().ContainSingle(progressEvent =>
            progressEvent.Stage == FileUploadProgressStage.SavingTransactions
            && progressEvent.Current == 2
            && progressEvent.Total == 2);
    }

    [Fact]
    public async Task ImportAsync_ShouldNotStoreImportWhenParserThrows()
    {
        var repository = new FakeTransactionRepository();
        var statementImportRepository = new FakeStatementImportRepository();
        var service = CreateService(
            repository,
            statementImportRepository,
            new ThrowingStatementParser());

        var act = async () => await service.ImportAsync(
            FixturePath("bank-of-america.csv"),
            new StatementFileImportOptions(),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        (await statementImportRepository.ListAsync(CancellationToken.None)).Should().BeEmpty();
        (await repository.ListAsync(CancellationToken.None)).Should().BeEmpty();
    }

    [Fact]
    public async Task ImportAsync_ShouldAllowRetryAfterZeroRowParseWithoutSavingFailedAttempt()
    {
        var repository = new FakeTransactionRepository();
        var statementImportRepository = new FakeStatementImportRepository();
        var filePath = FixturePath("bank-of-america.csv");
        var failingService = CreateService(repository, statementImportRepository, new EmptyStatementParser());

        var failedAttempt = async () => await failingService.ImportAsync(
            filePath,
            new StatementFileImportOptions(),
            CancellationToken.None);

        await failedAttempt.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Transaction header was not found.");
        (await statementImportRepository.ListAsync(CancellationToken.None)).Should().BeEmpty();

        var retryService = CreateService(repository, statementImportRepository);
        var result = await retryService.ImportAsync(filePath, new StatementFileImportOptions(), CancellationToken.None);

        result.SavedTransactionCount.Should().Be(2);
        (await statementImportRepository.ListAsync(CancellationToken.None)).Should().ContainSingle()
            .Which.Status.Should().Be(StatementImportStatus.Completed);
    }

    [Fact]
    public async Task ImportAsync_ShouldUseGenericMessageWhenZeroRowParseHasNoReason()
    {
        var repository = new FakeTransactionRepository();
        var statementImportRepository = new FakeStatementImportRepository();
        var service = CreateService(repository, statementImportRepository, new EmptyStatementParser(includeWarning: false));

        var act = async () => await service.ImportAsync(
            FixturePath("bank-of-america.csv"),
            new StatementFileImportOptions(),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No transactions could be parsed from the selected file.");
        (await statementImportRepository.ListAsync(CancellationToken.None)).Should().BeEmpty();
    }

    [Fact]
    public async Task ImportAsync_ShouldAllowRetryAfterLegacyCompletedZeroRowImport()
    {
        var repository = new FakeTransactionRepository();
        var statementImportRepository = new FakeStatementImportRepository();
        var filePath = FixturePath("bank-of-america.csv");
        var fileHash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(filePath)));
        await statementImportRepository.AddAsync(
            new StatementImport
            {
                CardAccountId = Guid.NewGuid(),
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                FileHash = fileHash,
                Status = StatementImportStatus.Completed
            },
            CancellationToken.None);

        var service = CreateService(repository, statementImportRepository);
        var result = await service.ImportAsync(filePath, new StatementFileImportOptions(), CancellationToken.None);

        result.SavedTransactionCount.Should().Be(2);
        (await statementImportRepository.ListAsync(CancellationToken.None)).Should().HaveCount(2);
    }

    [Fact]
    public async Task ImportAsync_ShouldAppendTransactionsAcrossImports()
    {
        var repository = new FakeTransactionRepository();
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
        var repository = new FakeTransactionRepository();
        var statementImportRepository = new FakeStatementImportRepository();
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
        var repository = new FakeTransactionRepository();
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
        var repository = new FakeTransactionRepository();
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
        var repository = new FakeTransactionRepository();
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

    [Fact]
    public async Task ImportAsync_ShouldSaveCategoryAssignmentsWithTransactions()
    {
        var repository = new FakeTransactionRepository();
        var assignments = new FakeTransactionCategoryAssignmentRepository();
        var service = CreateService(repository, assignmentRepository: assignments);

        var result = await service.ImportAsync(
            FixturePath("bank-of-america.csv"),
            new StatementFileImportOptions(),
            CancellationToken.None);

        var savedAssignments = await assignments.ListAsync(CancellationToken.None);
        result.Categorizations.Should().HaveCount(2);
        savedAssignments.Select(assignment => assignment.TransactionId)
            .Should().BeEquivalentTo(result.SavedTransactions.Select(transaction => transaction.Id));
        savedAssignments.Should().OnlyContain(assignment => assignment.CategoryId == BuiltInCategoryIds.Groceries);
    }

    [Fact]
    public async Task ImportAsync_ShouldFailWithoutSavingTransactionsWhenAnyTransactionIsUnresolved()
    {
        var repository = new FakeTransactionRepository();
        var statementImportRepository = new FakeStatementImportRepository();
        var assignments = new FakeTransactionCategoryAssignmentRepository();
        var categorization = new FakeCategorizationService { UnresolvedCount = 1 };
        var service = CreateService(repository, statementImportRepository, categorizationService: categorization, assignmentRepository: assignments);

        var act = async () => await service.ImportAsync(
            FixturePath("bank-of-america.csv"),
            new StatementFileImportOptions(),
            CancellationToken.None);

        await act.Should().ThrowAsync<StatementCategorizationFailedException>()
            .WithMessage("Categorization failed for 1 of 2 transactions, so no transactions were saved. AI categorization timed out.");
        (await repository.ListAsync(CancellationToken.None)).Should().BeEmpty();
        (await assignments.ListAsync(CancellationToken.None)).Should().BeEmpty();
        var statementImport = (await statementImportRepository.ListAsync(CancellationToken.None)).Should().ContainSingle().Subject;
        statementImport.Status.Should().Be(StatementImportStatus.Failed);
        statementImport.SavedTransactionCount.Should().Be(0);
        statementImport.ErrorMessage.Should().StartWith("Categorization failed for 1 of 2 transactions");
    }

    [Fact]
    public async Task ImportAsync_ShouldImportFailedFileAgain()
    {
        var repository = new FakeTransactionRepository();
        var statementImportRepository = new FakeStatementImportRepository();
        var filePath = FixturePath("bank-of-america.csv");
        var failingService = CreateService(
            repository,
            statementImportRepository,
            categorizationService: new FakeCategorizationService { UnresolvedCount = 2 });
        await FluentActions.Awaiting(() => failingService.ImportAsync(filePath, new StatementFileImportOptions(), CancellationToken.None))
            .Should().ThrowAsync<StatementCategorizationFailedException>();

        var result = await CreateService(repository, statementImportRepository)
            .ImportAsync(filePath, new StatementFileImportOptions(), CancellationToken.None);

        result.SavedTransactionCount.Should().Be(2);
        (await repository.ListAsync(CancellationToken.None)).Should().HaveCount(2);
        (await statementImportRepository.ListAsync(CancellationToken.None))
            .Select(statementImport => statementImport.Status)
            .Should().BeEquivalentTo([StatementImportStatus.Failed, StatementImportStatus.Completed]);
    }

    [Fact]
    public async Task ImportAsync_ShouldFailCancelledImportSoTheFileCanBeImportedAgain()
    {
        var repository = new FakeTransactionRepository();
        var statementImportRepository = new FakeStatementImportRepository();
        var filePath = FixturePath("bank-of-america.csv");
        var cancelledService = CreateService(
            repository,
            statementImportRepository,
            categorizationService: new FakeCategorizationService { Cancels = true });

        await FluentActions.Awaiting(() => cancelledService.ImportAsync(filePath, new StatementFileImportOptions(), CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();

        var cancelledImport = (await statementImportRepository.ListAsync(CancellationToken.None)).Should().ContainSingle().Subject;
        cancelledImport.Status.Should().Be(StatementImportStatus.Failed);
        cancelledImport.ErrorMessage.Should().Be("Import was cancelled.");
        (await repository.ListAsync(CancellationToken.None)).Should().BeEmpty();

        var result = await CreateService(repository, statementImportRepository)
            .ImportAsync(filePath, new StatementFileImportOptions(), CancellationToken.None);
        result.SavedTransactionCount.Should().Be(2);
    }

    [Fact]
    public async Task ImportAsync_ShouldFailImportWhenSavingFails()
    {
        var repository = new FakeTransactionRepository();
        var statementImportRepository = new FakeStatementImportRepository();
        var store = new FakeStatementImportStore(
            statementImportRepository,
            repository,
            new FakeTransactionCategoryAssignmentRepository())
        {
            FailWith = new InvalidOperationException("Disk is full.")
        };
        var service = CreateService(repository, statementImportRepository, store: store);

        var act = async () => await service.ImportAsync(
            FixturePath("bank-of-america.csv"),
            new StatementFileImportOptions(),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Disk is full.");
        var statementImport = (await statementImportRepository.ListAsync(CancellationToken.None)).Should().ContainSingle().Subject;
        statementImport.Status.Should().Be(StatementImportStatus.Failed);
        statementImport.ErrorMessage.Should().Be("Disk is full.");
        statementImport.SavedTransactionCount.Should().Be(0);
    }

    [Fact]
    public async Task ImportAsync_ShouldNotCategorizeWhenEveryRowIsADuplicate()
    {
        var repository = new FakeTransactionRepository();
        var categorization = new FakeCategorizationService();
        var service = CreateService(repository, categorizationService: categorization);
        var filePath = FixturePath("bank-of-america.csv");
        await service.ImportAsync(filePath, new StatementFileImportOptions(), CancellationToken.None);
        var copyPath = Path.GetTempFileName();
        await File.WriteAllTextAsync(copyPath, await File.ReadAllTextAsync(filePath) + Environment.NewLine);

        try
        {
            var result = await service.ImportAsync(copyPath, new StatementFileImportOptions(), CancellationToken.None);

            result.SavedTransactionCount.Should().Be(0);
            result.Categorizations.Should().BeEmpty();
            categorization.CallCount.Should().Be(1);
        }
        finally
        {
            File.Delete(copyPath);
        }
    }

    private static string FixturePath(string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, "Fixtures", "Csv", fileName);
    }

    private static StatementFileImportService CreateService(
        FakeTransactionRepository repository,
        FakeStatementImportRepository? statementImportRepository = null,
        IStatementParser? parser = null,
        ITransactionCategorizationService? categorizationService = null,
        FakeTransactionCategoryAssignmentRepository? assignmentRepository = null,
        IStatementImportStore? store = null)
    {
        statementImportRepository ??= new FakeStatementImportRepository();

        return new StatementFileImportService(
            parser ?? CreateStatementParser(),
            repository,
            new FakeCardAccountRepository(),
            statementImportRepository,
            store ?? new FakeStatementImportStore(
                statementImportRepository,
                repository,
                assignmentRepository ?? new FakeTransactionCategoryAssignmentRepository()),
            categorizationService ?? new FakeCategorizationService(),
            new LocalStatementFileReader());
    }

    private static IStatementParser CreateStatementParser()
    {
        return new ServiceCollection()
            .AddSpendnestStatementImporting()
            .BuildServiceProvider()
            .GetRequiredService<IStatementParser>();
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

    /// <summary>
    /// Categorizes every transaction as Groceries, leaving the first <see cref="UnresolvedCount"/> unresolved.
    /// </summary>
    private sealed class FakeCategorizationService : ITransactionCategorizationService
    {
        public int UnresolvedCount { get; init; }

        public bool Cancels { get; init; }

        public int CallCount { get; private set; }

        public Task<IReadOnlyList<TransactionCategorization>> CategorizeAsync(
            IReadOnlyList<Transaction> transactions,
            CancellationToken cancellationToken)
        {
            return CategorizeAsync(transactions, null, cancellationToken);
        }

        public Task<IReadOnlyList<TransactionCategorization>> CategorizeAsync(
            IReadOnlyList<Transaction> transactions,
            IProgress<FileUploadProgress>? progress,
            CancellationToken cancellationToken)
        {
            CallCount++;
            if (Cancels)
            {
                throw new OperationCanceledException();
            }

            return Task.FromResult<IReadOnlyList<TransactionCategorization>>(transactions
                .Select((transaction, index) => index < UnresolvedCount
                    ? new TransactionCategorization(
                        transaction.Id,
                        BuiltInCategoryIds.Other,
                        0m,
                        true,
                        CategorizationSource.Unresolved,
                        "AI categorization timed out.")
                    : new TransactionCategorization(
                        transaction.Id,
                        BuiltInCategoryIds.Groceries,
                        1m,
                        false,
                        CategorizationSource.LocalRules,
                        "Rule."))
                .ToArray());
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

    private sealed class EmptyStatementParser : IStatementParser
    {
        private readonly bool includeWarning;

        public EmptyStatementParser(bool includeWarning = true)
        {
            this.includeWarning = includeWarning;
        }

        public Task<StatementParseResult> ParseAsync(
            Stream stream,
            StatementParseOptions options,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new StatementParseResult(
                [],
                includeWarning ? [new StatementParseWarning(null, "Transaction header was not found.")] : [],
                0,
                0,
                includeWarning ? "Transaction header was not found." : null));
        }
    }
}
