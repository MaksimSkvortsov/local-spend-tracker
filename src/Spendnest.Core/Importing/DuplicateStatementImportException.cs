namespace Spendnest.Core.Importing;

/// <summary>
/// Raised when a statement file has already been imported from identical file contents.
/// </summary>
public sealed class DuplicateStatementImportException : InvalidOperationException
{
    public DuplicateStatementImportException(string fileName)
        : base($"'{fileName}' has already been imported.")
    {
        FileName = fileName;
    }

    public string FileName { get; }
}
