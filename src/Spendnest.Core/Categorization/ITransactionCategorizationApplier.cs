namespace Spendnest.Core.Categorization;

/// <summary>
/// Saves category results as transaction category assignments.
/// </summary>
public interface ITransactionCategorizationApplier
{
    Task ApplyAsync(
        IReadOnlyList<TransactionCategorization> categorizations,
        CancellationToken cancellationToken);
}
