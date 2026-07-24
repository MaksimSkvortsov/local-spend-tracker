namespace Spendnest.Core.Categorization;

/// <summary>
/// Stores user-learned local category rules for the active storage implementation.
/// </summary>
public interface ICategoryRuleRepository
{
    Task AddAsync(
        CategoryRule rule,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CategoryRule>> ListAsync(
        CancellationToken cancellationToken);
}
