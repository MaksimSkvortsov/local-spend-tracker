namespace Spendnest.Core.Categorization;

/// <summary>
/// Stores the current category assignment for one transaction.
/// </summary>
public sealed class TransactionCategoryAssignment
{
    public Guid TransactionId { get; init; }

    public int CategoryId { get; set; }

    public decimal Confidence { get; set; }

    public bool NeedsReview { get; set; }

    public CategorizationSource Source { get; set; }

    public string Explanation { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
