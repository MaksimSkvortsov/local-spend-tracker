using Spendnest.Core.Accounts;
using Spendnest.Core.Importing;
using Spendnest.Core.Transactions;

namespace Spendnest.Application.Cards;

public sealed class CardManagementService(
    ICardAccountRepository cardAccountRepository,
    ICardAccountManagementStore cardManagementStore,
    IStatementImportRepository statementImportRepository,
    ITransactionRepository transactionRepository)
{
    public async Task<CardManagementData> LoadAsync(CancellationToken cancellationToken)
    {
        var cards = await cardAccountRepository.ListAsync(cancellationToken);
        var transactions = await transactionRepository.ListAsync(cancellationToken);
        var imports = await statementImportRepository.ListAsync(cancellationToken);
        var transactionCounts = transactions
            .GroupBy(transaction => transaction.CardAccountId)
            .ToDictionary(group => group.Key, group => group.Count());
        var importsByCardId = imports
            .GroupBy(statementImport => statementImport.CardAccountId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var duplicateKeys = cards
            .GroupBy(card => NormalizeDuplicateName(card.Name))
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);
        var summaries = cards
            .Select(card =>
            {
                var cardImports = importsByCardId.GetValueOrDefault(card.Id) ?? [];

                var lastImportedDate = cardImports
                    .Select(item => item.CompletedAtUtc ?? item.StartedAtUtc)
                    .OrderByDescending(item => item)
                    .Select(item => (DateOnly?)DateOnly.FromDateTime(item.DateTime))
                    .FirstOrDefault();

                return new CardSummary(
                    card.Id,
                    card.Name,
                    transactionCounts.GetValueOrDefault(card.Id),
                    cardImports.Length,
                    lastImportedDate,
                    duplicateKeys.Contains(NormalizeDuplicateName(card.Name)));
            })
            .OrderBy(summary => summary.Name)
            .ToArray();

        return new CardManagementData(summaries, transactions.Count);
    }

    public async Task RenameAsync(
        CardRename rename,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rename);

        var normalizedName = NormalizeName(rename.Name);
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new InvalidOperationException("Card name is required.");
        }

        var cards = await cardAccountRepository.ListAsync(cancellationToken);
        var card = cards.FirstOrDefault(item => item.Id == rename.CardAccountId)
            ?? throw new InvalidOperationException("Card was not found.");
        if (cards.Any(item =>
            item.Id != card.Id
            && string.Equals(item.Name, normalizedName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Another card already uses that name.");
        }

        await cardManagementStore.RenameAsync(
            rename.CardAccountId,
            normalizedName,
            cancellationToken);
    }

    public async Task CombineAsync(
        CardCombine combine,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(combine);

        if (combine.SourceCardAccountId == Guid.Empty || combine.TargetCardAccountId == Guid.Empty)
        {
            throw new InvalidOperationException("Choose two cards to combine.");
        }

        if (combine.SourceCardAccountId == combine.TargetCardAccountId)
        {
            throw new InvalidOperationException("Choose two different cards to combine.");
        }

        var cards = await cardAccountRepository.ListAsync(cancellationToken);
        if (!cards.Any(card => card.Id == combine.SourceCardAccountId)
            || !cards.Any(card => card.Id == combine.TargetCardAccountId))
        {
            throw new InvalidOperationException("Card was not found.");
        }

        await cardManagementStore.CombineAsync(
            combine.SourceCardAccountId,
            combine.TargetCardAccountId,
            cancellationToken);
    }

    public async Task DeleteAsync(
        CardDelete delete,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(delete);

        if (delete.CardAccountId == Guid.Empty)
        {
            throw new InvalidOperationException("Choose a card to delete.");
        }

        var cards = await cardAccountRepository.ListAsync(cancellationToken);
        if (!cards.Any(card => card.Id == delete.CardAccountId))
        {
            throw new InvalidOperationException("Card was not found.");
        }

        await cardManagementStore.DeleteAsync(delete.CardAccountId, cancellationToken);
    }

    private static string NormalizeName(string name)
    {
        return name.Trim();
    }

    private static string NormalizeDuplicateName(string name)
    {
        return NormalizeName(name)
            .Replace(" Backup", string.Empty, StringComparison.OrdinalIgnoreCase)
            .ToUpperInvariant();
    }
}
