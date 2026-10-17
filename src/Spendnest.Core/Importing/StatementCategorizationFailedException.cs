namespace Spendnest.Core.Importing;

/// <summary>
/// Raised when some new transactions in a statement file could not be categorized, so nothing was saved.
/// </summary>
public sealed class StatementCategorizationFailedException : InvalidOperationException
{
    public StatementCategorizationFailedException(
        int unresolvedTransactionCount,
        int transactionCount,
        IReadOnlyList<string> reasons)
        : base(CreateMessage(unresolvedTransactionCount, transactionCount, reasons))
    {
        UnresolvedTransactionCount = unresolvedTransactionCount;
        TransactionCount = transactionCount;
    }

    public int UnresolvedTransactionCount { get; }

    public int TransactionCount { get; }

    private static string CreateMessage(
        int unresolvedTransactionCount,
        int transactionCount,
        IReadOnlyList<string> reasons)
    {
        var message = $"Categorization failed for {unresolvedTransactionCount} of {transactionCount} transactions, so no transactions were saved.";

        return reasons.Count == 0
            ? message
            : $"{message} {string.Join(" ", reasons)}";
    }
}
