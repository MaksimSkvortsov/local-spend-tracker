using Spendnest.Application.Categorization;
using Spendnest.Core.Accounts;
using Spendnest.Core.Categorization;
using Spendnest.Core.Importing;

namespace Spendnest.Application.Importing;

/// <summary>
/// Imports several statement files to one existing card, then categorizes all new transactions together.
/// A file that fails is reported and skipped; a file imported before is reported as already imported.
/// </summary>
public sealed class StatementFolderImportService(
    IStatementFileImportService importService,
    ICardAccountRepository cardAccountRepository,
    ITransactionCategorizationService categorizationService,
    ITransactionCategorizationApplier categorizationApplier)
{
    public async Task<StatementFolderImportResult> ImportAsync(
        StatementFolderImport request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CardName);

        var cardName = await ResolveCardNameAsync(request, cancellationToken);
        var files = new List<StatementFolderFileResult>();
        foreach (var filePath in request.FilePaths.Order(StringComparer.OrdinalIgnoreCase))
        {
            files.Add(await ImportFileAsync(filePath, cardName, cancellationToken));
        }

        var savedTransactions = files
            .SelectMany(file => file.Result?.SavedTransactions ?? [])
            .ToArray();
        IReadOnlyList<TransactionCategorization> categorizations = [];
        if (savedTransactions.Length > 0)
        {
            categorizations = await categorizationService.CategorizeAsync(savedTransactions, cancellationToken);
            await categorizationApplier.ApplyAsync(categorizations, cancellationToken);
        }

        return new StatementFolderImportResult(cardName, files, categorizations);
    }

    private async Task<string> ResolveCardNameAsync(
        StatementFolderImport request,
        CancellationToken cancellationToken)
    {
        var requestedName = request.CardName.Trim();
        var cards = await cardAccountRepository.ListAsync(cancellationToken);
        var card = cards.FirstOrDefault(item =>
            string.Equals(item.Name, requestedName, StringComparison.OrdinalIgnoreCase));

        if (card is not null)
        {
            return card.Name;
        }

        if (request.CreateCard)
        {
            return requestedName;
        }

        var existingNames = cards.Count == 0
            ? "none"
            : string.Join(", ", cards.Select(item => item.Name).Order(StringComparer.OrdinalIgnoreCase));
        throw new InvalidOperationException(
            $"No card named '{requestedName}'. Existing cards: {existingNames}. Rename the folder to a card name, or create the card with --new-card.");
    }

    private async Task<StatementFolderFileResult> ImportFileAsync(
        string filePath,
        string cardName,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await importService.ImportAsync(
                filePath,
                new StatementFileImportOptions { CardAccountName = cardName },
                cancellationToken);

            return new StatementFolderFileResult(filePath, StatementFolderFileStatus.Imported, result, null);
        }
        catch (DuplicateStatementImportException)
        {
            return new StatementFolderFileResult(filePath, StatementFolderFileStatus.AlreadyImported, null, null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new StatementFolderFileResult(filePath, StatementFolderFileStatus.Failed, null, exception.Message);
        }
    }
}
