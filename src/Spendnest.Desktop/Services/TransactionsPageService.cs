using Spendnest.Core.Accounts;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Review;
using Spendnest.Core.Transactions;
using Spendnest.Desktop.Presentation.Transactions;

namespace Spendnest.Desktop.Services;

public sealed class TransactionsPageService(
    ITransactionRepository transactionRepository,
    ITransactionCategoryAssignmentRepository assignmentRepository,
    ICardAccountRepository cardAccountRepository,
    ICategoryRepository categoryRepository,
    ITransactionReviewService reviewService)
{
    public async Task<TransactionsPageData> LoadAsync(CancellationToken cancellationToken)
    {
        var transactions = await transactionRepository.ListAsync(cancellationToken);
        var cards = await cardAccountRepository.ListAsync(cancellationToken);
        var categories = await categoryRepository.ListAsync(cancellationToken);
        var reviewState = await LoadReviewStateAsync(cancellationToken);

        return new TransactionsPageData(
            transactions,
            cards,
            categories,
            reviewState.AssignmentsByTransactionId,
            categories.ToDictionary(category => category.Id, category => category.ColorHex),
            cards.ToDictionary(card => card.Id, card => card.Name),
            reviewState.ReviewCount);
    }

    public async Task SetCategoryAsync(
        Guid transactionId,
        int categoryId,
        CancellationToken cancellationToken)
    {
        await reviewService.SetCategoryAsync(
            transactionId,
            categoryId,
            rememberRule: false,
            cancellationToken);
    }

    public async Task<TransactionsReviewState> LoadReviewStateAsync(CancellationToken cancellationToken)
    {
        var assignments = await assignmentRepository.ListAsync(cancellationToken);
        var reviewCount = await reviewService.CountNeedsReviewAsync(cancellationToken);

        return new TransactionsReviewState(
            assignments.ToDictionary(assignment => assignment.TransactionId),
            reviewCount);
    }
}
