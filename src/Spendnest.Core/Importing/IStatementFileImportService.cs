namespace Spendnest.Core.Importing;

/// <summary>
/// Imports a statement file by parsing and categorizing it, saving its transactions only when all are categorized.
/// </summary>
public interface IStatementFileImportService
{
    Task<StatementFileImportResult> ImportAsync(
        string filePath,
        StatementFileImportOptions options,
        CancellationToken cancellationToken);
}
