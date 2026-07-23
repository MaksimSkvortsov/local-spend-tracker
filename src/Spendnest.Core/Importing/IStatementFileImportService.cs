namespace Spendnest.Core.Importing;

/// <summary>
/// Imports a statement file by parsing it and saving normalized transactions.
/// </summary>
public interface IStatementFileImportService
{
    Task<StatementFileImportResult> ImportAsync(
        string filePath,
        CancellationToken cancellationToken);
}
