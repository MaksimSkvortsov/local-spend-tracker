using Microsoft.EntityFrameworkCore;
using Spendnest.Core.Categorization;
using Spendnest.Infrastructure.Persistence;

namespace Spendnest.Infrastructure.Categorization;

public sealed class SqliteCategoryRuleRepository :
    ICategoryRuleRepository,
    ICategoryRuleApplicationStore
{
    private readonly IDbContextFactory<SpendnestDbContext> dbContextFactory;

    public SqliteCategoryRuleRepository(IDbContextFactory<SpendnestDbContext> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
    }

    public async Task AddAsync(
        CategoryRule rule,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rule);

        await using var dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        dbContext.CategoryRules.Add(rule);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateCategoryAndAssignmentsAsync(
        Guid ruleId,
        string pattern,
        CategoryRuleMatchType matchType,
        int categoryId,
        IReadOnlyList<TransactionCategoryAssignment> assignments,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(assignments);

        await using var dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        var rule = await dbContext.CategoryRules
            .FirstOrDefaultAsync(item => item.Id == ruleId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Rule was not found.");

        rule.Pattern = pattern;
        rule.MatchType = matchType;
        rule.CategoryId = categoryId;

        foreach (var assignment in assignments)
        {
            var existingAssignment = await dbContext.TransactionCategoryAssignments
                .FirstOrDefaultAsync(
                    item => item.TransactionId == assignment.TransactionId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (existingAssignment is null)
            {
                dbContext.TransactionCategoryAssignments.Add(assignment);
            }
            else
            {
                dbContext.Entry(existingAssignment).CurrentValues.SetValues(assignment);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<CategoryRule>> ListAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        return await dbContext.CategoryRules
            .AsNoTracking()
            .OrderBy(rule => rule.Pattern)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
