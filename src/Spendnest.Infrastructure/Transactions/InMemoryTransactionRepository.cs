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
        cancellationToken.ThrowIfCancellationRequested();

        lock (transactions)
        {
            return Task.FromResult<IReadOnlyList<Transaction>>(transactions.ToArray());
        }
    }
}
