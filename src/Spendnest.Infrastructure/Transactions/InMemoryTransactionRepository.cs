using Spendnest.Core.Transactions;

namespace Spendnest.Infrastructure.Transactions;

/// <summary>
/// Stores transactions in memory for behavior-first development before SQLite exists.
/// </summary>
public sealed class InMemoryTransactionRepository : ITransactionRepository
{
    private readonly List<Transaction> transactions = [];

    public Task AddRangeAsync(
        IReadOnlyList<Transaction> transactionsToAdd,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (transactions)
        {
            transactions.AddRange(transactionsToAdd);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Transaction>> ListAsync(
        CancellationToken cancellationToken)
    {
        return ListAsync(new TransactionQuery(), cancellationToken);
    }

    public Task<IReadOnlyList<Transaction>> ListAsync(
        TransactionQuery query,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(query);

        lock (transactions)
        {
            var filteredTransactions = transactions
                .Where(transaction => query.StartDate is null || transaction.PostedDate >= query.StartDate)
                .Where(transaction => query.EndDate is null || transaction.PostedDate <= query.EndDate)
                .OrderBy(transaction => transaction.PostedDate)
                .ThenBy(transaction => transaction.OriginalDescription)
                .ToArray();

            return Task.FromResult<IReadOnlyList<Transaction>>(filteredTransactions);
        }
    }
}
