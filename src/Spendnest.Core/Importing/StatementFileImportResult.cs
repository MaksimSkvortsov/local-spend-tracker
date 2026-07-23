using Spendnest.Core.Transactions;

namespace Spendnest.Core.Importing;

/// <summary>
/// Summarizes the result of parsing and saving one statement file.
/// </summary>
public sealed record StatementFileImportResult(
    string FilePath,
    int ParsedRowCount,
    int SavedTransactionCount,
    int FailedRowCount,
    IReadOnlyList<Transaction> SavedTransactions,
    IReadOnlyList<StatementParseWarning> Warnings);
