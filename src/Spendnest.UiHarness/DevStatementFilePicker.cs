using Spendnest.Desktop.Services;

namespace Spendnest.UiHarness;

/// <summary>
/// Stands in for the native file dialog by returning a preselected statement file.
/// The file comes from <c>UiHarness:StatementFile</c> or the harness statement-file endpoint;
/// with no file selected, picking behaves like a cancelled dialog.
/// </summary>
public sealed class DevStatementFilePicker : IStatementFilePicker
{
    public const string StatementFileKey = "UiHarness:StatementFile";

    private PickedStatementFile? selectedFile;

    public DevStatementFilePicker(IConfiguration configuration)
    {
        var configuredPath = configuration[StatementFileKey];

        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            Select(configuredPath);
        }
    }

    public Task<PickedStatementFile?> PickCsvAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Volatile.Read(ref selectedFile));
    }

    public PickedStatementFile Select(string path)
    {
        if (!Path.IsPathFullyQualified(path))
        {
            throw new ArgumentException($"Statement file path must be absolute: '{path}'.", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Statement file for the UI harness was not found.", path);
        }

        var file = new PickedStatementFile(Path.GetFileName(path), path);
        Volatile.Write(ref selectedFile, file);
        return file;
    }

    public void Clear()
    {
        Volatile.Write(ref selectedFile, null);
    }
}
