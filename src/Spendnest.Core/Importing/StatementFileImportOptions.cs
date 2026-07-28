namespace Spendnest.Core.Importing;

using Spendnest.Core.Progress;

/// <summary>
/// Provides import settings that are known before parsing a statement file.
/// </summary>
public sealed class StatementFileImportOptions
{
    public string CardAccountName { get; init; } = "Default Card";

    public IProgress<FileUploadProgress>? Progress { get; init; }
}
