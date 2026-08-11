using Microsoft.EntityFrameworkCore;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Persistence;

namespace Spendnest.Infrastructure.Transactions;

public sealed class SqliteTransactionRepository : ITransactionRepository
{
    private readonly IDbContextFactory<SpendnestDbContext> dbContextFactory;

    public SqliteTransactionRepository(IDbContextFactory<SpendnestDbContext> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
    }

    public async Task AddRangeAsync(
        IReadOnlyList<Transaction> transactions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transactions);

        await using var dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        dbContext.Transactions.AddRange(transactions);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Transaction?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        return await dbContext.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(transaction => transaction.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<IReadOnlyList<Transaction>> ListAsync(CancellationToken cancellationToken)
    {
        return ListAsync(new TransactionQuery(), cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> ListAsync(
        TransactionQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        await using var dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var transactions = dbContext.Transactions.AsNoTracking();

        if (query.StartDate is not null)
        {
            transactions = transactions.Where(transaction => transaction.PostedDate >= query.StartDate);
        }

        if (query.EndDate is not null)
        {
            transactions = transactions.Where(transaction => transaction.PostedDate <= query.EndDate);
        }

        return await transactions
            .OrderBy(transaction => transaction.PostedDate)
            .ThenBy(transaction => transaction.OriginalDescription)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
