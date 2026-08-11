using Microsoft.EntityFrameworkCore;
using Spendnest.Core.Categories;
using Spendnest.Infrastructure.Persistence;

namespace Spendnest.Infrastructure.Categories;

public sealed class SqliteCategoryRepository : ICategoryRepository
{
    private readonly IDbContextFactory<SpendnestDbContext> dbContextFactory;

    public SqliteCategoryRepository(IDbContextFactory<SpendnestDbContext> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<BuiltInCategory>> ListAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var categories = await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.SortOrder)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        return categories
            .Select(ToBuiltInCategory)
            .ToArray();
    }

    public async Task<BuiltInCategory?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var category = await dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(
                category => category.Id == id && category.IsActive,
                cancellationToken)
            .ConfigureAwait(false);

        return category is null ? null : ToBuiltInCategory(category);
    }

    private static BuiltInCategory ToBuiltInCategory(Category category)
    {
        var builtInCategory = BuiltInCategories.All.FirstOrDefault(item => item.Id == category.Id);
        return new BuiltInCategory(
            category.Id,
            category.Name,
            category.SortOrder,
            builtInCategory?.ColorHex ?? "#e5e7e2");
    }
}
