using Spendnest.Core.Accounts;

namespace Spendnest.Infrastructure.Accounts;

/// <summary>
/// Stores card accounts in memory until SQLite persistence is added.
/// </summary>
public sealed class InMemoryCardAccountRepository : ICardAccountRepository
{
    private readonly List<CardAccount> cardAccounts = [];

    public Task<CardAccount?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedName = NormalizeName(name);

        lock (cardAccounts)
        {
            var existingAccount = cardAccounts.FirstOrDefault(account =>
                string.Equals(account.Name, normalizedName, StringComparison.OrdinalIgnoreCase));

            if (existingAccount is not null)
            {
                return Task.FromResult<CardAccount?>(existingAccount);
            }

            return Task.FromResult<CardAccount?>(null);
        }
    }

    public Task<CardAccount> CreateAsync(
        string name,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedName = NormalizeName(name);

        lock (cardAccounts)
        {
            var account = new CardAccount
            {
                Name = normalizedName,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            cardAccounts.Add(account);

            return Task.FromResult(account);
        }
    }

    public Task<IReadOnlyList<CardAccount>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (cardAccounts)
        {
            return Task.FromResult<IReadOnlyList<CardAccount>>(cardAccounts.ToArray());
        }
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Default Card";
        }

        return name.Trim();
    }
}
