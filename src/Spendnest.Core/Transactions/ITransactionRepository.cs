namespace Spendnest.Core.Transactions;

/// <summary>
/// Stores imported transactions for the current application storage implementation.
/// </summary>
public interface ITransactionRepository
{
    Task AddRangeAsync(
        IReadOnlyList<Transaction> transactions,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Transaction>> ListAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Transaction>> ListAsync(
        TransactionQuery query,
        CancellationToken cancellationToken);
}
