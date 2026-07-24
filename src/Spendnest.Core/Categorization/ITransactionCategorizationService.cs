using Spendnest.Core.Transactions;

namespace Spendnest.Core.Categorization;

/// <summary>
/// Coordinates local and AI categorization for a batch of transactions.
/// </summary>
public interface ITransactionCategorizationService
{
    Task<IReadOnlyList<TransactionCategorization>> CategorizeAsync(
        IReadOnlyList<Transaction> transactions,
        CancellationToken cancellationToken);
}
