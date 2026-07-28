using Spendnest.Core.Transactions;
using Spendnest.Core.Progress;

namespace Spendnest.Core.Categorization;

/// <summary>
/// Coordinates local and AI categorization for a batch of transactions.
/// </summary>
public interface ITransactionCategorizationService
{
    Task<IReadOnlyList<TransactionCategorization>> CategorizeAsync(
        IReadOnlyList<Transaction> transactions,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TransactionCategorization>> CategorizeAsync(
        IReadOnlyList<Transaction> transactions,
        IProgress<FileUploadProgress>? progress,
        CancellationToken cancellationToken);
}
