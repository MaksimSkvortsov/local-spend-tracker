using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Review;
using Spendnest.Core.Transactions;

namespace Spendnest.Application.Review;

/// <summary>
/// Manages user review of transaction category assignments.
/// </summary>
public sealed class TransactionReviewService : ITransactionReviewService
{
    private readonly ITransactionRepository transactionRepository;
    private readonly ITransactionCategoryAssignmentRepository assignmentRepository;
    private readonly ICategoryRuleRepository categoryRuleRepository;
    private readonly ITransactionMerchantCodeResolver merchantCodeResolver;

    public TransactionReviewService(
        ITransactionRepository transactionRepository,
        ITransactionCategoryAssignmentRepository assignmentRepository,
        ICategoryRuleRepository categoryRuleRepository,
        ITransactionMerchantCodeResolver merchantCodeResolver)
    {
        this.transactionRepository = transactionRepository;
        this.assignmentRepository = assignmentRepository;
        this.categoryRuleRepository = categoryRuleRepository;
        this.merchantCodeResolver = merchantCodeResolver;
    }

    public async Task<IReadOnlyList<TransactionReviewItem>> ListNeedsReviewAsync(CancellationToken cancellationToken)
    {
        var reviewEntries = await ListReviewEntriesAsync(cancellationToken).ConfigureAwait(false);

        return reviewEntries
            .Select(ToReviewItem)
            .ToArray();
    }

    public async Task<int> CountNeedsReviewAsync(CancellationToken cancellationToken)
    {
        var reviewEntries = await ListReviewEntriesAsync(cancellationToken).ConfigureAwait(false);

        return reviewEntries.Count;
    }

    public async Task ConfirmAsync(
        Guid transactionId,
        bool rememberRule,
        CancellationToken cancellationToken)
    {
        var transaction = await GetTransactionAsync(transactionId, cancellationToken).ConfigureAwait(false);
        var assignment = await GetAssignmentAsync(transactionId, cancellationToken).ConfigureAwait(false);
        if (assignment.CategoryId == 0)
        {
            throw new InvalidOperationException("Transaction does not have a category to confirm.");
        }

        CompleteReview(assignment, assignment.CategoryId, "Confirmed during review.");

        await assignmentRepository.SaveAsync(assignment, cancellationToken).ConfigureAwait(false);

        if (rememberRule)
        {
            await RememberExactRuleAsync(transaction, assignment.CategoryId, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task SetCategoryAsync(
        Guid transactionId,
        int categoryId,
        bool rememberRule,
        CancellationToken cancellationToken)
    {
        ValidateCategoryId(categoryId);

        var transaction = await GetTransactionAsync(transactionId, cancellationToken).ConfigureAwait(false);
        var assignment = new TransactionCategoryAssignment
        {
            TransactionId = transactionId
        };
        CompleteReview(assignment, categoryId, "Set during review.");

        await assignmentRepository.SaveAsync(assignment, cancellationToken).ConfigureAwait(false);

        if (rememberRule)
        {
            await RememberExactRuleAsync(transaction, categoryId, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<Transaction> GetTransactionAsync(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        var transaction = await transactionRepository.GetByIdAsync(transactionId, cancellationToken).ConfigureAwait(false);

        return transaction ?? throw new InvalidOperationException($"Transaction '{transactionId}' was not found.");
    }

    private async Task<TransactionCategoryAssignment> GetAssignmentAsync(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        var assignment = await assignmentRepository.GetByTransactionIdAsync(transactionId, cancellationToken).ConfigureAwait(false);

        return assignment ?? throw new InvalidOperationException($"Transaction '{transactionId}' does not have a category assignment.");
    }

    private async Task<IReadOnlyList<(Transaction Transaction, TransactionCategoryAssignment Assignment)>> ListReviewEntriesAsync(
        CancellationToken cancellationToken)
    {
        var transactions = await transactionRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        var transactionsById = transactions.ToDictionary(transaction => transaction.Id);
        var assignments = await assignmentRepository.ListAsync(cancellationToken).ConfigureAwait(false);

        return assignments
            .Where(assignment => assignment.NeedsReview && transactionsById.ContainsKey(assignment.TransactionId))
            .Select(assignment => (transactionsById[assignment.TransactionId], assignment))
            .ToArray();
    }

    private static TransactionReviewItem ToReviewItem(
        (Transaction Transaction, TransactionCategoryAssignment Assignment) entry)
    {
        return new TransactionReviewItem(
            entry.Assignment.TransactionId,
            entry.Transaction.PostedDate,
            entry.Transaction.OriginalDescription,
            entry.Transaction.Amount,
            entry.Assignment.CategoryId,
            entry.Assignment.Source,
            entry.Assignment.Confidence,
            entry.Assignment.Explanation);
    }

    private async Task RememberExactRuleAsync(
        Transaction transaction,
        int categoryId,
        CancellationToken cancellationToken)
    {
        await categoryRuleRepository.AddAsync(
            new CategoryRule
            {
                Pattern = merchantCodeResolver.Resolve(transaction),
                CategoryId = categoryId,
                MatchType = CategoryRuleMatchType.Exact
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static void CompleteReview(
        TransactionCategoryAssignment assignment,
        int categoryId,
        string explanation)
    {
        assignment.CategoryId = categoryId;
        assignment.NeedsReview = false;
        assignment.Source = CategorizationSource.LocalRules;
        assignment.Confidence = 1m;
        assignment.Explanation = explanation;
        assignment.UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static void ValidateCategoryId(int categoryId)
    {
        var isValid = BuiltInCategories.All.Any(category => category.Id == categoryId);
        if (!isValid)
        {
            throw new ArgumentException($"Unknown category id '{categoryId}'.", nameof(categoryId));
        }
    }
}
