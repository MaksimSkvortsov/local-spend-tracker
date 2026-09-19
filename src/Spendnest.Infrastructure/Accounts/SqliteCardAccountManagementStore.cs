using Microsoft.EntityFrameworkCore;
using Spendnest.Core.Accounts;
using Spendnest.Infrastructure.Persistence;

namespace Spendnest.Infrastructure.Accounts;

public sealed class SqliteCardAccountManagementStore : ICardAccountManagementStore
{
    private readonly IDbContextFactory<SpendnestDbContext> dbContextFactory;

    public SqliteCardAccountManagementStore(IDbContextFactory<SpendnestDbContext> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
    }

    public async Task RenameAsync(
        Guid cardAccountId,
        string name,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var updatedCount = await dbContext.CardAccounts
            .Where(card => card.Id == cardAccountId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(card => card.Name, name),
                cancellationToken)
            .ConfigureAwait(false);
        if (updatedCount == 0)
        {
            throw new InvalidOperationException("Card was not found.");
        }
    }

    public async Task CombineAsync(
        Guid sourceCardAccountId,
        Guid targetCardAccountId,
        CancellationToken cancellationToken)
    {
        if (sourceCardAccountId == targetCardAccountId)
        {
            throw new InvalidOperationException("Choose two different cards to combine.");
        }

        await using var dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        var sourceExists = await dbContext.CardAccounts
            .AnyAsync(card => card.Id == sourceCardAccountId, cancellationToken)
            .ConfigureAwait(false);
        var targetExists = await dbContext.CardAccounts
            .AnyAsync(card => card.Id == targetCardAccountId, cancellationToken)
            .ConfigureAwait(false);
        if (!sourceExists || !targetExists)
        {
            throw new InvalidOperationException("Card was not found.");
        }

        await dbContext.Transactions
            .Where(item => item.CardAccountId == sourceCardAccountId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.CardAccountId, targetCardAccountId),
                cancellationToken)
            .ConfigureAwait(false);
        await dbContext.StatementImports
            .Where(item => item.CardAccountId == sourceCardAccountId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.CardAccountId, targetCardAccountId),
                cancellationToken)
            .ConfigureAwait(false);
        await dbContext.CardAccounts
            .Where(card => card.Id == sourceCardAccountId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(
        Guid cardAccountId,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        var transactionIds = await dbContext.Transactions
            .Where(item => item.CardAccountId == cardAccountId)
            .Select(item => item.Id)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        await dbContext.TransactionCategoryAssignments
            .Where(assignment => transactionIds.Contains(assignment.TransactionId))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await dbContext.Transactions
            .Where(item => item.CardAccountId == cardAccountId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await dbContext.StatementImports
            .Where(item => item.CardAccountId == cardAccountId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        var deletedCount = await dbContext.CardAccounts
            .Where(card => card.Id == cardAccountId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        if (deletedCount == 0)
        {
            throw new InvalidOperationException("Card was not found.");
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }
}
