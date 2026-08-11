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
                    && statementImport.Status != StatementImportStatus.Failed,
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
