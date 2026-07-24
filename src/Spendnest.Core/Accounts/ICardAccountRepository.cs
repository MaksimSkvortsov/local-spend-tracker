namespace Spendnest.Core.Accounts;

/// <summary>
/// Stores credit-card accounts for the active storage implementation.
/// </summary>
public interface ICardAccountRepository
{
    Task<CardAccount?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken);

    Task<CardAccount> CreateAsync(
        string name,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CardAccount>> ListAsync(
        CancellationToken cancellationToken);
}
