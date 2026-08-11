using Microsoft.EntityFrameworkCore;
using Spendnest.Core.Categorization;
using Spendnest.Infrastructure.Persistence;

namespace Spendnest.Infrastructure.Categorization;

public sealed class SqliteTransactionCategoryAssignmentRepository :
    ITransactionCategoryAssignmentRepository
{
    private readonly IDbContextFactory<SpendnestDbContext> dbContextFactory;

    public SqliteTransactionCategoryAssignmentRepository(
        IDbContextFactory<SpendnestDbContext> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
    }

    public async Task SaveAsync(
        TransactionCategoryAssignment assignment,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        await using var dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingAssignment = await dbContext.TransactionCategoryAssignments
            .FirstOrDefaultAsync(
                item => item.TransactionId == assignment.TransactionId,
                cancellationToken)
            .ConfigureAwait(false);

        if (existingAssignment is null)
        {
            dbContext.TransactionCategoryAssignments.Add(assignment);
        }
        else
        {
            dbContext.Entry(existingAssignment).CurrentValues.SetValues(assignment);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<TransactionCategoryAssignment?> GetByTransactionIdAsync(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        return await dbContext.TransactionCategoryAssignments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                assignment => assignment.TransactionId == transactionId,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TransactionCategoryAssignment>> ListAsync(
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        return await dbContext.TransactionCategoryAssignments
            .AsNoTracking()
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
