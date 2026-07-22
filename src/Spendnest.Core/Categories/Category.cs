namespace Spendnest.Core.Categories;

/// <summary>
/// Represents one built-in spending category with a stable code.
/// Custom categories are outside the MVP.
/// </summary>
public sealed class Category
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
