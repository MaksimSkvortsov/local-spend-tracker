using Spendnest.Core.Credentials;

namespace Spendnest.App.Infrastructure.Credentials;

public sealed class SecureStorageCredentialStore : ICredentialStore
{
    public async Task SaveStringAsync(
        string key,
        string value,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        await SecureStorage.Default
            .SetAsync(key, value.Trim())
            .ConfigureAwait(false);
    }

    public async Task<string?> GetStringAsync(
        string key,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return await SecureStorage.Default
            .GetAsync(key)
            .ConfigureAwait(false);
    }

    public Task ClearAsync(
        string key,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        SecureStorage.Default.Remove(key);
        return Task.CompletedTask;
    }
}
