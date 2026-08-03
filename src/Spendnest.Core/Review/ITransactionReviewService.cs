namespace Spendnest.Core.Review;

/// <summary>
/// Provides review actions for transactions with uncertain categorization.
/// </summary>
public interface ITransactionReviewService
{
    Task<IReadOnlyList<TransactionReviewItem>> ListNeedsReviewAsync(CancellationToken cancellationToken);

    Task<int> CountNeedsReviewAsync(CancellationToken cancellationToken);

    Task ConfirmAsync(
        Guid transactionId,
        bool rememberRule,
        CancellationToken cancellationToken);

    Task SetCategoryAsync(
        Guid transactionId,
        int categoryId,
        bool rememberRule,
        CancellationToken cancellationToken);
}
