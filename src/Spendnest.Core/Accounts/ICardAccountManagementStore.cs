namespace Spendnest.Core.Accounts;

/// <summary>
/// Persists card management changes that must update related records together.
/// </summary>
public interface ICardAccountManagementStore
{
    Task RenameAsync(
        Guid cardAccountId,
        string name,
        CancellationToken cancellationToken);

    Task CombineAsync(
        Guid sourceCardAccountId,
        Guid targetCardAccountId,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        Guid cardAccountId,
        CancellationToken cancellationToken);
}
