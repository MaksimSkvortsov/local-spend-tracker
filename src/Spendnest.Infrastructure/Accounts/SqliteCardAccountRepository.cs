using Microsoft.EntityFrameworkCore;
using Spendnest.Core.Accounts;
using Spendnest.Infrastructure.Persistence;

namespace Spendnest.Infrastructure.Accounts;

public sealed class SqliteCardAccountRepository : ICardAccountRepository
{
    private readonly IDbContextFactory<SpendnestDbContext> dbContextFactory;

    public SqliteCardAccountRepository(IDbContextFactory<SpendnestDbContext> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
    }

    public async Task<CardAccount?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken)
    {
        var normalizedName = NormalizeName(name);
        await using var dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        return await dbContext.CardAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                account => account.Name.ToUpper() == normalizedName.ToUpper(),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<CardAccount> CreateAsync(
        string name,
        CancellationToken cancellationToken)
    {
        var account = new CardAccount
        {
            Name = NormalizeName(name),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        await using var dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        dbContext.CardAccounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return account;
    }

    public async Task<IReadOnlyList<CardAccount>> ListAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        return await dbContext.CardAccounts
            .AsNoTracking()
            .OrderBy(account => account.Name)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static string NormalizeName(string name)
    {
        return string.IsNullOrWhiteSpace(name)
            ? "Default Card"
            : name.Trim();
    }
}
