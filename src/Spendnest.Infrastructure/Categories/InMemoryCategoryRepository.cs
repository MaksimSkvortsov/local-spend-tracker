using Spendnest.Core.Categories;

namespace Spendnest.Infrastructure.Categories;

/// <summary>
/// Fake category database seeded with the built-in MVP categories.
/// </summary>
public sealed class InMemoryCategoryRepository : ICategoryRepository
{
    private readonly List<BuiltInCategory> categories = [.. BuiltInCategories.All];

    public Task<IReadOnlyList<BuiltInCategory>> ListAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<BuiltInCategory>>(
            categories
                .OrderBy(category => category.SortOrder)
                .ToArray());
    }

    public Task<BuiltInCategory?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(categories.FirstOrDefault(category => category.Id == id));
    }
}
