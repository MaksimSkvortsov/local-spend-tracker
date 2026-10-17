using Microsoft.EntityFrameworkCore;
using Spendnest.Core.Categorization;
using Spendnest.Core.Importing;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Persistence;

namespace Spendnest.Infrastructure.Importing;

public sealed class SqliteStatementImportStore : IStatementImportStore
{
    private readonly IDbContextFactory<SpendnestDbContext> dbContextFactory;

    public SqliteStatementImportStore(IDbContextFactory<SpendnestDbContext> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
    }

    public async Task CompleteImportAsync(
        StatementImport statementImport,
        IReadOnlyList<Transaction> transactions,
        IReadOnlyList<TransactionCategoryAssignment> assignments,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(statementImport);
        ArgumentNullException.ThrowIfNull(transactions);
        ArgumentNullException.ThrowIfNull(assignments);

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

        dbContext.Transactions.AddRange(transactions);
        dbContext.TransactionCategoryAssignments.AddRange(assignments);

        // A single SaveChanges call runs in one database transaction.
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
