namespace Spendnest.Core.Categories;

/// <summary>
/// Stores transaction categories for the active storage implementation.
/// </summary>
public interface ICategoryRepository
{
    Task<IReadOnlyList<BuiltInCategory>> ListAsync(
        CancellationToken cancellationToken);

    Task<BuiltInCategory?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);
}
