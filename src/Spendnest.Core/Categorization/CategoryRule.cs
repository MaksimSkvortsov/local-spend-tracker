namespace Spendnest.Core.Categorization;

/// <summary>
/// Represents a local rule that maps transaction description text to a category.
/// </summary>
public sealed class CategoryRule
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Pattern { get; init; } = string.Empty;

    public int CategoryId { get; init; }

    public CategoryRuleMatchType MatchType { get; init; } = CategoryRuleMatchType.Contains;
}
