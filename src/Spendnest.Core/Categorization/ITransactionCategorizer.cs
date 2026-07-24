using Spendnest.Core.Transactions;

namespace Spendnest.Core.Categorization;

/// <summary>
/// Categorizes transactions using a local or AI-backed implementation.
/// </summary>
public interface ITransactionCategorizer
{
    Task<IReadOnlyList<TransactionCategorization>> CategorizeAsync(
        IReadOnlyList<Transaction> transactions,
        CancellationToken cancellationToken);
}
