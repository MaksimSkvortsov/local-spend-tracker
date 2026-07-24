using Spendnest.Core.Transactions;

namespace Spendnest.Core.Categorization;

/// <summary>
/// Categorizes only transactions that local rules can identify confidently.
/// </summary>
public interface ILocalTransactionCategorizer
{
    Task<IReadOnlyList<TransactionCategorization>> CategorizeKnownAsync(
        IReadOnlyList<Transaction> transactions,
        CancellationToken cancellationToken);
}
