using Spendnest.Core.Categorization;

namespace Spendnest.Infrastructure.Categorization;

/// <summary>
/// Stores transaction category assignments in memory until SQLite persistence is added.
/// </summary>
public sealed class InMemoryTransactionCategoryAssignmentRepository : ITransactionCategoryAssignmentRepository
{
    private readonly Dictionary<Guid, TransactionCategoryAssignment> assignments = [];

    public Task SaveAsync(
        TransactionCategoryAssignment assignment,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        cancellationToken.ThrowIfCancellationRequested();

        lock (assignments)
        {
            assignments[assignment.TransactionId] = assignment;
        }

        return Task.CompletedTask;
    }

    public Task<TransactionCategoryAssignment?> GetByTransactionIdAsync(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (assignments)
        {
            return Task.FromResult(assignments.GetValueOrDefault(transactionId));
        }
    }

    public Task<IReadOnlyList<TransactionCategoryAssignment>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (assignments)
        {
            return Task.FromResult<IReadOnlyList<TransactionCategoryAssignment>>(assignments.Values.ToArray());
        }
    }
}
