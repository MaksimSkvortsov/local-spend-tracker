namespace Spendnest.Desktop.Services;

public interface IStatementFilePicker
{
    Task<PickedStatementFile?> PickCsvAsync(CancellationToken cancellationToken);
}

public sealed record PickedStatementFile(
    string FileName,
    string LocalPath);
