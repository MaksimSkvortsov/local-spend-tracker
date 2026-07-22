namespace Spendnest.Core.Categories;

/// <summary>
/// Describes one built-in category before it is persisted.
/// </summary>
public sealed record BuiltInCategory(
    string Code,
    string Name,
    int SortOrder);
