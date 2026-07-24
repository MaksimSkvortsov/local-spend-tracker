namespace Spendnest.Core.Accounts;

/// <summary>
/// Represents one credit card whose statement transactions can be imported.
/// </summary>
public sealed class CardAccount
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; init; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
