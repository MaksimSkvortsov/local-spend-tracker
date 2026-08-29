using Spendnest.Core.Importing;
using Spendnest.Core.Transactions;

namespace Spendnest.Desktop.Presentation.Importing;

public sealed record UploadHistoryItem(
    DateTimeOffset ImportedAtLocal,
    string FileName,
    string CardAccountName,
    int ParsedRowCount,
    int SavedTransactionCount,
    int SkippedDuplicateTransactionCount,
    int FailedRowCount,
    int CategorizedTransactionCount,
    int NeedsReviewCount,
    int TimeoutCount,
    string TransactionRange,
    StatementImportStatus Status)
{
    public string StatusLabel => Status.ToString();

    public static UploadHistoryItem From(
        StatementImport statementImport,
        string cardAccountName,
        CategorizationHistorySummary? categorizationSummary,
        IReadOnlyList<Transaction> transactions)
    {
        var transactionRange = BuildTransactionRange(transactions);

        return new UploadHistoryItem(
            (statementImport.CompletedAtUtc ?? statementImport.StartedAtUtc).ToLocalTime(),
            statementImport.FileName,
            cardAccountName,
            statementImport.ParsedRowCount,
            statementImport.SavedTransactionCount,
            statementImport.SkippedDuplicateTransactionCount,
            statementImport.FailedRowCount,
            categorizationSummary?.CategorizedTransactionCount ?? 0,
            categorizationSummary?.NeedsReviewCount ?? 0,
            categorizationSummary?.TimeoutCount ?? 0,
            transactionRange,
            statementImport.Status);
    }

    private static string BuildTransactionRange(IReadOnlyList<Transaction> transactions)
    {
        if (transactions.Count == 0)
        {
            return "--";
        }

        var start = transactions.Min(transaction => transaction.PostedDate);
        var end = transactions.Max(transaction => transaction.PostedDate);

        return start == end
            ? start.ToString("MMM d, yyyy")
            : $"{start:MMM d, yyyy} - {end:MMM d, yyyy}";
    }
}
