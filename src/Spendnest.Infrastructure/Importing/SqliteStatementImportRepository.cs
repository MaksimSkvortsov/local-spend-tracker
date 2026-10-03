using Microsoft.EntityFrameworkCore;
using Spendnest.Core.Importing;
using Spendnest.Infrastructure.Persistence;

namespace Spendnest.Infrastructure.Importing;

public sealed class SqliteStatementImportRepository : IStatementImportRepository
{
    private readonly IDbContextFactory<SpendnestDbContext> dbContextFactory;

    public SqliteStatementImportRepository(IDbContextFactory<SpendnestDbContext> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
    }

    public async Task AddAsync(
        StatementImport statementImport,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(statementImport);

        await using var dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        dbContext.StatementImports.Add(statementImport);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(
        StatementImport statementImport,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(statementImport);

        await using var dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingImport = await dbContext.StatementImports
            .FirstOrDefaultAsync(item => item.Id == statementImport.Id, cancellationToken)
            .ConfigureAwait(false);

        if (existingImport is null)
        {
            dbContext.StatementImports.Add(statementImport);
        }
        else
        {
            dbContext.Entry(existingImport).CurrentValues.SetValues(statementImport);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(
        Guid statementImportId,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        var transactionIds = await dbContext.Transactions
            .Where(item => item.StatementImportId == statementImportId)
            .Select(item => item.Id)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        await dbContext.TransactionCategoryAssignments
            .Where(assignment => transactionIds.Contains(assignment.TransactionId))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await dbContext.Transactions
            .Where(item => item.StatementImportId == statementImportId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        var deletedCount = await dbContext.StatementImports
            .Where(item => item.Id == statementImportId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        if (deletedCount == 0)
        {
            throw new InvalidOperationException("Import was not found.");
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<StatementImport?> GetByFileHashAsync(
        string fileHash,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        return await dbContext.StatementImports
            .AsNoTracking()
            .FirstOrDefaultAsync(
                statementImport =>
                    statementImport.FileHash.ToUpper() == fileHash.ToUpper()
                    && (statementImport.Status == StatementImportStatus.Pending
                        || (statementImport.Status == StatementImportStatus.Completed
                            && statementImport.ParsedRowCount > 0)),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<StatementImport>> ListAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var statementImports = await dbContext.StatementImports
            .AsNoTracking()
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        return statementImports
            .OrderByDescending(statementImport => statementImport.StartedAtUtc)
            .ToArray();
    }
}
