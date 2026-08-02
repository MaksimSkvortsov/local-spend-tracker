namespace Spendnest.Core.Categories;

/// <summary>
/// Describes one built-in category before it is persisted.
/// </summary>
public sealed record BuiltInCategory(
    int Id,
    string Name,
    int SortOrder,
    string ColorHex);
