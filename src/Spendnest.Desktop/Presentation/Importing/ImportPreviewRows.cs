using Spendnest.Core.Importing;
using Spendnest.Core.Transactions;

namespace Spendnest.Desktop.Presentation.Importing;

public static class ImportPreviewRows
{
    public static IReadOnlyList<PreviewRow> FromParseResult(StatementParseResult? parseResult)
    {
        return parseResult?.Rows
            .Select(row => new PreviewRow(
                row.PostedDate,
                row.OriginalDescription,
                string.IsNullOrWhiteSpace(row.SourceCategory) ? "Will categorize" : row.SourceCategory,
                row.Amount))
            .ToArray() ?? [];
    }

    public static IReadOnlyList<PreviewRow> FromImportResult(
        StatementFileImportResult importResult,
        IReadOnlyDictionary<Guid, string> categoryNamesByTransactionId,
        bool isCategorizing)
    {
        return importResult.SavedTransactions
            .Select(transaction => new PreviewRow(
                transaction.PostedDate,
                transaction.OriginalDescription,
                GetTransactionCategory(transaction, categoryNamesByTransactionId, isCategorizing),
                transaction.Amount))
            .ToArray();
    }

    private static string GetTransactionCategory(
        Transaction transaction,
        IReadOnlyDictionary<Guid, string> categoryNamesByTransactionId,
        bool isCategorizing)
    {
        if (isCategorizing)
        {
            return "Pending";
        }

        return categoryNamesByTransactionId.GetValueOrDefault(transaction.Id, "Needs review");
    }
}
