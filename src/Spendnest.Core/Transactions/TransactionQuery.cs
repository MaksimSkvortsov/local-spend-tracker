namespace Spendnest.Core.Transactions;

/// <summary>
/// Defines filters for reading transactions from storage.
/// </summary>
public sealed class TransactionQuery
{
    public DateOnly? StartDate { get; init; }

    public DateOnly? EndDate { get; init; }
}
