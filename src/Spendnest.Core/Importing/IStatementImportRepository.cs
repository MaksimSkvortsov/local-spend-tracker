namespace Spendnest.Core.Importing;

/// <summary>
/// Stores statement import history for the active storage implementation.
/// </summary>
public interface IStatementImportRepository
{
    Task AddAsync(
        StatementImport statementImport,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        StatementImport statementImport,
        CancellationToken cancellationToken);

    Task<StatementImport?> GetByFileHashAsync(
        string fileHash,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<StatementImport>> ListAsync(
        CancellationToken cancellationToken);
}
