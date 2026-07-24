using Spendnest.Core.Credentials;

namespace Spendnest.Infrastructure.Credentials;

/// <summary>
/// Stores credentials in memory for debugging until secure storage is added.
/// </summary>
public sealed class InMemoryCredentialStore : ICredentialStore
{
    private readonly object gate = new();
    private readonly Dictionary<string, string> values = new(StringComparer.Ordinal);

    public InMemoryCredentialStore(IReadOnlyDictionary<string, string?>? initialValues = null)
    {
        if (initialValues is null)
        {
            return;
        }

        foreach (var (key, value) in initialValues)
        {
            var normalizedValue = NormalizeValue(value);
            if (normalizedValue is not null)
            {
                values[key] = normalizedValue;
            }
        }
    }

    public Task SaveStringAsync(
        string key,
        string value,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var normalizedValue = NormalizeValue(value);
        if (normalizedValue is null)
        {
            throw new ArgumentException("Credential value is required.", nameof(value));
        }

        lock (gate)
        {
            values[key] = normalizedValue;
        }

        return Task.CompletedTask;
    }

    public Task<string?> GetStringAsync(
        string key,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        lock (gate)
        {
            return Task.FromResult(values.GetValueOrDefault(key));
        }
    }

    public Task ClearAsync(
        string key,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        lock (gate)
        {
            values.Remove(key);
        }

        return Task.CompletedTask;
    }

    private static string? NormalizeValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
