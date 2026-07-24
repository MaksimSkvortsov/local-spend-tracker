using Spendnest.Core.Categorization;

namespace Spendnest.Infrastructure.Categorization;

/// <summary>
/// Stores local category rules in memory until SQLite persistence is added.
/// </summary>
public sealed class InMemoryCategoryRuleRepository : ICategoryRuleRepository
{
    private readonly List<CategoryRule> rules = [];

    public Task AddAsync(
        CategoryRule rule,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rule);
        cancellationToken.ThrowIfCancellationRequested();

        lock (rules)
        {
            rules.Add(rule);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CategoryRule>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (rules)
        {
            return Task.FromResult<IReadOnlyList<CategoryRule>>(rules.ToArray());
        }
    }
}
