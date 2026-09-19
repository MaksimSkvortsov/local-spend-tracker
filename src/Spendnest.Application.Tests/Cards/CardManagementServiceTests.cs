namespace Spendnest.Application.Tests.Cards;

using FluentAssertions;
using Spendnest.Application.Cards;
using Spendnest.Application.Tests.TestDoubles;
using Spendnest.Core.Importing;
using Spendnest.Core.Transactions;

public class CardManagementServiceTests
{
    [Fact]
    public async Task LoadAsync_ShouldReturnCardsWithCounts()
    {
        var cardAccounts = new FakeCardAccountRepository();
        var store = new FakeCardAccountManagementStore();
        var statementImports = new FakeStatementImportRepository();
        var transactions = new FakeTransactionRepository();
        var service = new CardManagementService(cardAccounts, store, statementImports, transactions);
        var card = await cardAccounts.CreateAsync("Family Visa", CancellationToken.None);
        var statementImport = StatementImport(card.Id, new DateTimeOffset(2026, 9, 9, 10, 0, 0, TimeSpan.Zero));
        await statementImports.AddAsync(statementImport, CancellationToken.None);
        await transactions.AddRangeAsync(
            [
                Transaction(card.Id, statementImport.Id, "GROCERY MART"),
                Transaction(card.Id, statementImport.Id, "COFFEE SHOP")
            ],
            CancellationToken.None);

        var data = await service.LoadAsync(CancellationToken.None);

        data.TotalTransactionCount.Should().Be(2);
        data.Cards.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new CardSummary(
                card.Id,
                "Family Visa",
                2,
                1,
                new DateOnly(2026, 9, 9),
                false));
    }

    [Fact]
    public async Task RenameAsync_ShouldTrimNameAndRejectDuplicates()
    {
        var cardAccounts = new FakeCardAccountRepository();
        var store = new FakeCardAccountManagementStore();
        var service = new CardManagementService(
            cardAccounts,
            store,
            new FakeStatementImportRepository(),
            new FakeTransactionRepository());
        var card = await cardAccounts.CreateAsync("Family Visa", CancellationToken.None);
        await cardAccounts.CreateAsync("Travel Amex", CancellationToken.None);

        await service.RenameAsync(
            new CardRename(card.Id, "  Family Rewards Visa  "),
            CancellationToken.None);

        store.RenamedCard.Should().Be(new CardRename(card.Id, "Family Rewards Visa"));
        var renameDuplicate = async () => await service.RenameAsync(
            new CardRename(card.Id, "travel amex"),
            CancellationToken.None);
        await renameDuplicate.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Another card already uses that name.");
    }

    [Fact]
    public async Task CombineAsync_ShouldRejectSameCard()
    {
        var service = new CardManagementService(
            new FakeCardAccountRepository(),
            new FakeCardAccountManagementStore(),
            new FakeStatementImportRepository(),
            new FakeTransactionRepository());
        var cardId = Guid.NewGuid();

        var combine = async () => await service.CombineAsync(
            new CardCombine(cardId, cardId),
            CancellationToken.None);

        await combine.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Choose two different cards to combine.");
    }

    private static StatementImport StatementImport(
        Guid cardAccountId,
        DateTimeOffset completedAtUtc)
    {
        return new StatementImport
        {
            CardAccountId = cardAccountId,
            FilePath = "statement.csv",
            FileName = "statement.csv",
            FileHash = Guid.NewGuid().ToString("N"),
            Status = StatementImportStatus.Completed,
            StartedAtUtc = completedAtUtc.AddMinutes(-1),
            CompletedAtUtc = completedAtUtc
        };
    }

    private static Transaction Transaction(
        Guid cardAccountId,
        Guid statementImportId,
        string description)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            CardAccountId = cardAccountId,
            StatementImportId = statementImportId,
            PostedDate = new DateOnly(2026, 9, 9),
            OriginalDescription = description,
            Amount = 12.34m,
            SourceRowNumber = 2,
            ImportedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
