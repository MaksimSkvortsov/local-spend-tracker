namespace Spendnest.Core.Categorization;

/// <summary>
/// Stores current category assignments separately from transaction facts.
/// </summary>
public interface ITransactionCategoryAssignmentRepository
{
    Task SaveAsync(
        TransactionCategoryAssignment assignment,
        CancellationToken cancellationToken);

    Task<TransactionCategoryAssignment?> GetByTransactionIdAsync(
        Guid transactionId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TransactionCategoryAssignment>> ListAsync(CancellationToken cancellationToken);
}
