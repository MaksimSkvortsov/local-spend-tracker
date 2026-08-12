namespace Spendnest.Application.Importing;

public interface IStatementFileReader
{
    Task<StatementFileReadResult> OpenReadAsync(
        string filePath,
        CancellationToken cancellationToken);
}
