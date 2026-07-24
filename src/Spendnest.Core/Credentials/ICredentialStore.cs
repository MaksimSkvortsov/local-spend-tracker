namespace Spendnest.Core.Credentials;

/// <summary>
/// Stores small credential values by stable application keys.
/// </summary>
public interface ICredentialStore
{
    Task SaveStringAsync(
        string key,
        string value,
        CancellationToken cancellationToken);

    Task<string?> GetStringAsync(
        string key,
        CancellationToken cancellationToken);

    Task ClearAsync(
        string key,
        CancellationToken cancellationToken);
}
