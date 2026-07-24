using Spendnest.Core.Transactions;

namespace Spendnest.Core.Importing;

/// <summary>
/// Summarizes the result of parsing and saving one statement file.
/// </summary>
public sealed record StatementFileImportResult(
    string FilePath,
    Guid CardAccountId,
    string CardAccountName,
    int ParsedRowCount,
    int SavedTransactionCount,
    int SkippedDuplicateTransactionCount,
    int FailedRowCount,
    IReadOnlyList<Transaction> SavedTransactions,
    IReadOnlyList<StatementParseWarning> Warnings);
