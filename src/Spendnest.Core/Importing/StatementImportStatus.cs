namespace Spendnest.Core.Importing;

/// <summary>
/// Describes the current outcome of a statement import attempt.
/// </summary>
public enum StatementImportStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3
}
