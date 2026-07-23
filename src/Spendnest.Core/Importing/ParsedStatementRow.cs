namespace Spendnest.Core.Importing;

/// <summary>
/// Represents one parsed and normalized transaction row from a statement file.
/// </summary>
public sealed record ParsedStatementRow(
    DateOnly PostedDate,
    string OriginalDescription,
    decimal Amount,
    int SourceRowNumber);
