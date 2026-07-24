namespace Spendnest.Core.Transactions;

/// <summary>
/// Represents one normalized transaction imported from a statement file.
/// </summary>
public sealed class Transaction
{
    public Guid Id { get; set; }

    public Guid CardAccountId { get; set; }

    public DateOnly PostedDate { get; set; }

    public string OriginalDescription { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public int SourceRowNumber { get; set; }

    public DateTimeOffset ImportedAtUtc { get; set; }
}
