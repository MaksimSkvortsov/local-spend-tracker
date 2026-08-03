using Spendnest.Core.Importing;

namespace Spendnest.Infrastructure.Importing;

/// <summary>
/// Stores statement import history in memory until SQLite persistence is added.
/// </summary>
public sealed class InMemoryStatementImportRepository : IStatementImportRepository
{
    private readonly List<StatementImport> statementImports = [];

    public Task AddAsync(
        StatementImport statementImport,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(statementImport);

        lock (statementImports)
        {
            statementImports.Add(statementImport);
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(
        StatementImport statementImport,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(statementImport);

        lock (statementImports)
        {
            var index = statementImports.FindIndex(existingImport => existingImport.Id == statementImport.Id);
            if (index < 0)
            {
                statementImports.Add(statementImport);
                return Task.CompletedTask;
            }

            statementImports[index] = statementImport;
        }

        return Task.CompletedTask;
    }

    public Task<StatementImport?> GetByFileHashAsync(
        string fileHash,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (statementImports)
        {
            return Task.FromResult(statementImports.FirstOrDefault(statementImport =>
                statementImport.FileHash.Equals(fileHash, StringComparison.OrdinalIgnoreCase)
                && statementImport.Status != StatementImportStatus.Failed));
        }
    }

    public Task<IReadOnlyList<StatementImport>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (statementImports)
        {
            var imports = statementImports
                .OrderByDescending(statementImport => statementImport.StartedAtUtc)
                .ToArray();

            return Task.FromResult<IReadOnlyList<StatementImport>>(imports);
        }
    }
}
