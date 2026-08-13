using Spendnest.Core.Categorization;

namespace Spendnest.Application.Categorization;

/// <summary>
/// Saves category results as transaction category assignments.
/// </summary>
public sealed class TransactionCategorizationApplier : ITransactionCategorizationApplier
{
    private readonly ITransactionCategoryAssignmentRepository assignmentRepository;

    public TransactionCategorizationApplier(ITransactionCategoryAssignmentRepository assignmentRepository)
    {
        this.assignmentRepository = assignmentRepository;
    }

    public async Task ApplyAsync(
        IReadOnlyList<TransactionCategorization> categorizations,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(categorizations);

        foreach (var categorization in categorizations)
        {
            await assignmentRepository.SaveAsync(
                CreateAssignment(categorization),
                cancellationToken).ConfigureAwait(false);
        }
    }

    private static TransactionCategoryAssignment CreateAssignment(TransactionCategorization categorization)
    {
        return new TransactionCategoryAssignment
        {
            TransactionId = categorization.TransactionId,
            CategoryId = categorization.CategoryId,
            Confidence = categorization.Confidence,
            NeedsReview = categorization.NeedsReview,
            Source = categorization.Source,
            Explanation = categorization.Explanation,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
