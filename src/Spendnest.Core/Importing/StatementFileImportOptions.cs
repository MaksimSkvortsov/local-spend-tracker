namespace Spendnest.Core.Importing;

/// <summary>
/// Provides import settings that are known before parsing a statement file.
/// </summary>
public sealed class StatementFileImportOptions
{
    public string CardAccountName { get; init; } = "Default Card";
}
