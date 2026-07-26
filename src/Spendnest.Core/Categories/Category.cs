namespace Spendnest.Core.Categories;

/// <summary>
/// Represents one built-in spending category.
/// Custom categories are outside the MVP.
/// </summary>
public sealed class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
