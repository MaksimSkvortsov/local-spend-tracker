using Spendnest.Core.Accounts;
using Spendnest.Core.Importing;
using Spendnest.Core.Transactions;
using Spendnest.Desktop.Presentation.Importing;

namespace Spendnest.Desktop.Services;

public sealed class ImportPageService(
    ITransactionRepository transactionRepository,
    IStatementImportRepository importHistoryRepository,
    ICardAccountRepository cardAccountRepository)
{
    public async Task<ImportPageData> LoadAsync(
        IReadOnlyDictionary<Guid, CategorizationHistorySummary> categorizationSummariesByImportId,
        CancellationToken cancellationToken)
    {
        var transactions = await transactionRepository.ListAsync(cancellationToken);
        var cards = await cardAccountRepository.ListAsync(cancellationToken);
        var imports = await importHistoryRepository.ListAsync(cancellationToken);

        var cardsById = cards.ToDictionary(card => card.Id, card => card.Name);
        var transactionsByImportId = transactions
            .GroupBy(transaction => transaction.StatementImportId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<Transaction>)group.ToArray());

        var uploadHistory = imports
            .Select(statementImport => UploadHistoryItem.From(
                statementImport,
                cardsById.GetValueOrDefault(statementImport.CardAccountId, "Unknown Card"),
                categorizationSummariesByImportId.GetValueOrDefault(statementImport.Id),
                transactionsByImportId.GetValueOrDefault(statementImport.Id, [])))
            .ToArray();

        return new ImportPageData(cards, uploadHistory);
    }
}
