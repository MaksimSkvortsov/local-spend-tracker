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
                categorization.ToAssignment(DateTimeOffset.UtcNow),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
