using Microsoft.EntityFrameworkCore;
using Spendnest.Core.Categorization;
using Spendnest.Infrastructure.Persistence;

namespace Spendnest.Infrastructure.Categorization;

public sealed class SqliteCategoryRuleRepository : ICategoryRuleRepository
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
