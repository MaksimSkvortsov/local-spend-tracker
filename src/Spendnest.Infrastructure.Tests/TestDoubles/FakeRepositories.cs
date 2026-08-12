namespace Spendnest.Infrastructure.Tests.TestDoubles;

using Spendnest.Core.Categorization;

public sealed class FakeCategoryRuleRepository : ICategoryRuleRepository
{
    private readonly List<CategoryRule> rules = [];

    public Task AddAsync(
        CategoryRule rule,
        CancellationToken cancellationToken)
    {
        rules.Add(rule);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CategoryRule>> ListAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<CategoryRule>>(rules.ToArray());
    }
}

public sealed class FakeTransactionCategoryAssignmentRepository : ITransactionCategoryAssignmentRepository
{
    private readonly Dictionary<Guid, TransactionCategoryAssignment> assignments = [];

    public Task SaveAsync(
        TransactionCategoryAssignment assignment,
        CancellationToken cancellationToken)
    {
        assignments[assignment.TransactionId] = assignment;

        return Task.CompletedTask;
    }

    public Task<TransactionCategoryAssignment?> GetByTransactionIdAsync(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(assignments.GetValueOrDefault(transactionId));
    }

    public Task<IReadOnlyList<TransactionCategoryAssignment>> ListAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<TransactionCategoryAssignment>>(assignments.Values.ToArray());
    }
}
