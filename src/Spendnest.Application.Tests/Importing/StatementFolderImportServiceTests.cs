namespace Spendnest.Application.Tests.Importing;

using FluentAssertions;
using Spendnest.Application.Categorization;
using Spendnest.Application.Importing;
using Spendnest.Application.Tests.TestDoubles;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Importing;
using Spendnest.Core.Progress;
using Spendnest.Core.Transactions;

public class StatementFolderImportServiceTests
{
    [Fact]
    public async Task ImportAsync_ShouldImportFilesInOrderToMatchingCardAndCategorizeOnce()
    {
        var cards = new FakeCardAccountRepository();
        await cards.CreateAsync("BoA Debit", CancellationToken.None);
        var importService = new FakeFileImportService();
        importService.Saves("b.csv", 2);
        importService.Saves("a.csv", 3);
        var categorization = new FakeCategorizationService();
        var applier = new FakeCategorizationApplier();
        var service = new StatementFolderImportService(importService, cards, categorization, applier);

        var result = await service.ImportAsync(
            new StatementFolderImport("boa debit", ["b.csv", "a.csv"], false),
            CancellationToken.None);

        result.CardName.Should().Be("BoA Debit");
        importService.ImportedPaths.Should().Equal("a.csv", "b.csv");
        importService.CardNames.Should().AllBe("BoA Debit");
        result.Files.Select(file => file.Status).Should().AllBeEquivalentTo(StatementFolderFileStatus.Imported);
        categorization.Calls.Should().ContainSingle().Which.Should().HaveCount(5);
        applier.Applied.Should().HaveCount(5);
    }

    [Fact]
    public async Task ImportAsync_ShouldReportAlreadyImportedAndFailedFilesAndContinue()
    {
        var cards = new FakeCardAccountRepository();
        await cards.CreateAsync("Cap1", CancellationToken.None);
        var importService = new FakeFileImportService();
        importService.Throws("a.csv", new DuplicateStatementImportException("a.csv"));
        importService.Throws("b.csv", new InvalidOperationException("No transactions could be parsed."));
        importService.Saves("c.csv", 1);
        var categorization = new FakeCategorizationService();
        var service = new StatementFolderImportService(importService, cards, categorization, new FakeCategorizationApplier());

        var result = await service.ImportAsync(
            new StatementFolderImport("Cap1", ["a.csv", "b.csv", "c.csv"], false),
            CancellationToken.None);

        result.Files.Select(file => file.Status).Should().Equal(
            StatementFolderFileStatus.AlreadyImported,
            StatementFolderFileStatus.Failed,
            StatementFolderFileStatus.Imported);
        result.Files[1].Error.Should().Be("No transactions could be parsed.");
        categorization.Calls.Should().ContainSingle().Which.Should().HaveCount(1);
    }

    [Fact]
    public async Task ImportAsync_ShouldRejectUnknownCardWithoutImporting()
    {
        var cards = new FakeCardAccountRepository();
        await cards.CreateAsync("Cap1", CancellationToken.None);
        await cards.CreateAsync("Boa", CancellationToken.None);
        var importService = new FakeFileImportService();
        var service = new StatementFolderImportService(
            importService,
            cards,
            new FakeCategorizationService(),
            new FakeCategorizationApplier());

        var act = () => service.ImportAsync(
            new StatementFolderImport("Chase", ["a.csv"], false),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No card named 'Chase'. Existing cards: Boa, Cap1.*");
        importService.ImportedPaths.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportAsync_ShouldUseFolderNameForNewCardWhenRequested()
    {
        var importService = new FakeFileImportService();
        importService.Saves("a.csv", 1);
        var service = new StatementFolderImportService(
            importService,
            new FakeCardAccountRepository(),
            new FakeCategorizationService(),
            new FakeCategorizationApplier());

        var result = await service.ImportAsync(
            new StatementFolderImport(" Chase ", ["a.csv"], true),
            CancellationToken.None);

        result.CardName.Should().Be("Chase");
        importService.CardNames.Should().Equal("Chase");
    }

    [Fact]
    public async Task ImportAsync_ShouldSkipCategorizationWhenNothingWasSaved()
    {
        var cards = new FakeCardAccountRepository();
        await cards.CreateAsync("Cap1", CancellationToken.None);
        var importService = new FakeFileImportService();
        importService.Throws("a.csv", new DuplicateStatementImportException("a.csv"));
        var categorization = new FakeCategorizationService();
        var service = new StatementFolderImportService(importService, cards, categorization, new FakeCategorizationApplier());

        var result = await service.ImportAsync(
            new StatementFolderImport("Cap1", ["a.csv"], false),
            CancellationToken.None);

        result.Categorizations.Should().BeEmpty();
        categorization.Calls.Should().BeEmpty();
    }

    private sealed class FakeFileImportService : IStatementFileImportService
    {
        private readonly Dictionary<string, Func<string, StatementFileImportResult>> behaviors = [];

        public List<string> ImportedPaths { get; } = [];

        public List<string> CardNames { get; } = [];

        public void Saves(string filePath, int transactionCount)
        {
            behaviors[filePath] = cardName => Result(filePath, cardName, transactionCount);
        }

        public void Throws(string filePath, Exception exception)
        {
            behaviors[filePath] = _ => throw exception;
        }

        public Task<StatementFileImportResult> ImportAsync(
            string filePath,
            StatementFileImportOptions options,
            CancellationToken cancellationToken)
        {
            ImportedPaths.Add(filePath);
            CardNames.Add(options.CardAccountName);
            return Task.FromResult(behaviors[filePath](options.CardAccountName));
        }

        private static StatementFileImportResult Result(
            string filePath,
            string cardName,
            int transactionCount)
        {
            var transactions = Enumerable.Range(0, transactionCount)
                .Select(index => new Transaction
                {
                    Id = Guid.NewGuid(),
                    OriginalDescription = $"MERCHANT {index}",
                    Amount = 10m,
                    PostedDate = new DateOnly(2025, 4, 1)
                })
                .ToArray();

            return new StatementFileImportResult(
                Guid.NewGuid(),
                filePath,
                Guid.NewGuid(),
                cardName,
                transactionCount,
                transactionCount,
                0,
                0,
                transactions,
                []);
        }
    }

    private sealed class FakeCategorizationService : ITransactionCategorizationService
    {
        public List<IReadOnlyList<Transaction>> Calls { get; } = [];

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
            Calls.Add(transactions);
            return Task.FromResult<IReadOnlyList<TransactionCategorization>>(transactions
                .Select(transaction => new TransactionCategorization(
                    transaction.Id,
                    BuiltInCategoryIds.Other,
                    1m,
                    false,
                    CategorizationSource.LocalRules,
                    "Test."))
                .ToArray());
        }
    }

    private sealed class FakeCategorizationApplier : ITransactionCategorizationApplier
    {
        public List<TransactionCategorization> Applied { get; } = [];

        public Task ApplyAsync(
            IReadOnlyList<TransactionCategorization> categorizations,
            CancellationToken cancellationToken)
        {
            Applied.AddRange(categorizations);
            return Task.CompletedTask;
        }
    }
}
