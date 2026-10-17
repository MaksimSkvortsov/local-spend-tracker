namespace Spendnest.Application.Tests.Importing;

using FluentAssertions;
using Spendnest.Application.Importing;
using Spendnest.Application.Tests.TestDoubles;
using Spendnest.Core.Importing;

public class StatementImportRetryServiceTests
{
    [Fact]
    public async Task RetryAsync_ShouldImportFileOntoSameCardAndReplaceFailedRow()
    {
        var harness = await Harness.CreateAsync();
        var failedImport = await harness.AddFailedImportAsync("statement.csv");
        harness.ImportService.Behavior = (path, options) => harness.CompleteImportAsync(path, options);

        var result = await harness.Service.RetryAsync(failedImport.Id, null, CancellationToken.None);

        harness.ImportService.Calls.Should().ContainSingle()
            .Which.Should().Be(("statement.csv", "Travel Visa"));
        var imports = await harness.Imports.ListAsync(CancellationToken.None);
        imports.Should().ContainSingle().Which.Id.Should().Be(result.StatementImportId);
    }

    [Fact]
    public async Task RetryAsync_ShouldDeleteOldFailedRowWhenAttemptFailsAgain()
    {
        var harness = await Harness.CreateAsync();
        var failedImport = await harness.AddFailedImportAsync("statement.csv");
        var newAttemptId = Guid.NewGuid();
        harness.ImportService.Behavior = async (path, _) =>
        {
            await harness.Imports.AddAsync(
                new StatementImport
                {
                    Id = newAttemptId,
                    FilePath = path,
                    Status = StatementImportStatus.Failed,
                    ErrorMessage = "AI categorization timed out."
                },
                CancellationToken.None);
            throw new StatementCategorizationFailedException(1, 1, ["AI categorization timed out."]);
        };

        var act = () => harness.Service.RetryAsync(failedImport.Id, null, CancellationToken.None);

        await act.Should().ThrowAsync<StatementCategorizationFailedException>();
        (await harness.Imports.ListAsync(CancellationToken.None))
            .Should().ContainSingle().Which.Id.Should().Be(newAttemptId);
    }

    [Fact]
    public async Task RetryAsync_ShouldKeepFailedRowWhenFileIsMissing()
    {
        var harness = await Harness.CreateAsync();
        var failedImport = await harness.AddFailedImportAsync(@"C:\missing\statement.csv");
        harness.ImportService.Behavior = (_, _) => throw new FileNotFoundException("Missing.");

        var act = () => harness.Service.RetryAsync(failedImport.Id, null, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(@"File not found at C:\missing\statement.csv. Import it again from the Import page.");
        (await harness.Imports.ListAsync(CancellationToken.None))
            .Should().ContainSingle().Which.Id.Should().Be(failedImport.Id);
    }

    [Fact]
    public async Task RetryAsync_ShouldKeepFailedRowWhenAttemptFailsBeforeRecordingAnImport()
    {
        var harness = await Harness.CreateAsync();
        var failedImport = await harness.AddFailedImportAsync("statement.csv");
        harness.ImportService.Behavior = (_, _) => throw new IOException("The file is open in another program.");

        var act = () => harness.Service.RetryAsync(failedImport.Id, null, CancellationToken.None);

        await act.Should().ThrowAsync<IOException>();
        (await harness.Imports.ListAsync(CancellationToken.None))
            .Should().ContainSingle().Which.Id.Should().Be(failedImport.Id);
    }

    [Theory]
    [InlineData(StatementImportStatus.Completed)]
    [InlineData(StatementImportStatus.Pending)]
    public async Task RetryAsync_ShouldRejectImportsThatDidNotFail(StatementImportStatus status)
    {
        var harness = await Harness.CreateAsync();
        var statementImport = await harness.AddFailedImportAsync("statement.csv");
        statementImport.Status = status;

        var act = () => harness.Service.RetryAsync(statementImport.Id, null, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Only failed imports can be retried.");
        harness.ImportService.Calls.Should().BeEmpty();
    }

    private sealed class Harness
    {
        private Harness(Guid cardAccountId, FakeCardAccountRepository cards)
        {
            CardAccountId = cardAccountId;
            Service = new StatementImportRetryService(ImportService, Imports, cards);
        }

        public Guid CardAccountId { get; }

        public FakeStatementImportRepository Imports { get; } = new();

        public RecordingImportService ImportService { get; } = new();

        public StatementImportRetryService Service { get; }

        public static async Task<Harness> CreateAsync()
        {
            var cards = new FakeCardAccountRepository();
            await cards.CreateAsync("Other Card", CancellationToken.None);
            var card = await cards.CreateAsync("Travel Visa", CancellationToken.None);

            return new Harness(card.Id, cards);
        }

        public async Task<StatementImport> AddFailedImportAsync(string filePath)
        {
            var statementImport = new StatementImport
            {
                CardAccountId = CardAccountId,
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                Status = StatementImportStatus.Failed,
                ErrorMessage = "AI categorization timed out."
            };
            await Imports.AddAsync(statementImport, CancellationToken.None);

            return statementImport;
        }

        public async Task<StatementFileImportResult> CompleteImportAsync(
            string filePath,
            StatementFileImportOptions options)
        {
            var statementImport = new StatementImport
            {
                CardAccountId = CardAccountId,
                FilePath = filePath,
                Status = StatementImportStatus.Completed
            };
            await Imports.AddAsync(statementImport, CancellationToken.None);

            return new StatementFileImportResult(
                statementImport.Id,
                filePath,
                CardAccountId,
                options.CardAccountName,
                0,
                0,
                0,
                0,
                [],
                [],
                []);
        }
    }

    private sealed class RecordingImportService : IStatementFileImportService
    {
        public List<(string FilePath, string CardAccountName)> Calls { get; } = [];

        public Func<string, StatementFileImportOptions, Task<StatementFileImportResult>> Behavior { get; set; } =
            (_, _) => throw new InvalidOperationException("No behavior configured.");

        public Task<StatementFileImportResult> ImportAsync(
            string filePath,
            StatementFileImportOptions options,
            CancellationToken cancellationToken)
        {
            Calls.Add((filePath, options.CardAccountName));
            return Behavior(filePath, options);
        }
    }
}
