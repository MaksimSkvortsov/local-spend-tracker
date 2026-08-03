using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Review;
using Spendnest.Core.Transactions;

namespace Spendnest.Infrastructure.Review;

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
        var transactions = await transactionRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        var transactionsById = transactions.ToDictionary(transaction => transaction.Id);
        var assignments = await assignmentRepository.ListAsync(cancellationToken).ConfigureAwait(false);

        return assignments
            .Where(assignment => assignment.NeedsReview && transactionsById.ContainsKey(assignment.TransactionId))
            .Select(assignment =>
            {
                var transaction = transactionsById[assignment.TransactionId];

                return new TransactionReviewItem(
                    assignment.TransactionId,
                    transaction.PostedDate,
                    transaction.OriginalDescription,
                    transaction.Amount,
                    assignment.CategoryId,
                    assignment.Source,
                    assignment.Confidence,
                    assignment.Explanation);
            })
            .ToArray();
    }

    public async Task<int> CountNeedsReviewAsync(CancellationToken cancellationToken)
    {
        var transactions = await transactionRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        var transactionIds = transactions.Select(transaction => transaction.Id).ToHashSet();
        var assignments = await assignmentRepository.ListAsync(cancellationToken).ConfigureAwait(false);

        return assignments.Count(assignment =>
            assignment.NeedsReview && transactionIds.Contains(assignment.TransactionId));
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

        assignment.NeedsReview = false;
        assignment.Source = CategorizationSource.LocalRules;
        assignment.Confidence = 1m;
        assignment.Explanation = "Confirmed during review.";
        assignment.UpdatedAtUtc = DateTimeOffset.UtcNow;

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
        await assignmentRepository.SaveAsync(
            new TransactionCategoryAssignment
            {
                TransactionId = transactionId,
                CategoryId = categoryId,
                NeedsReview = false,
                Source = CategorizationSource.LocalRules,
                Confidence = 1m,
                Explanation = "Set during review.",
                UpdatedAtUtc = DateTimeOffset.UtcNow
            },
            cancellationToken).ConfigureAwait(false);

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

    private static void ValidateCategoryId(int categoryId)
    {
        var isValid = BuiltInCategories.All.Any(category => category.Id == categoryId);
        if (!isValid)
        {
            throw new ArgumentException($"Unknown category id '{categoryId}'.", nameof(categoryId));
        }
    }
}
