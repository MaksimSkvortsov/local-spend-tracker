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
        var assignments = await assignmentRepository.ListAsync(cancellationToken);
        var assignmentsByTransactionId = assignments.ToDictionary(assignment => assignment.TransactionId);
        var categoryColorsById = categories.ToDictionary(category => category.Id, category => category.ColorHex);
        var cardNamesById = cards.ToDictionary(card => card.Id, card => card.Name);
        var reviewCount = await reviewService.CountNeedsReviewAsync(cancellationToken);

        return new TransactionsPageData(
            TransactionRows.FromTransactions(
                transactions,
                assignmentsByTransactionId,
                categoryColorsById,
                cardNamesById),
            cards,
            categories,
            reviewCount);
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

}
