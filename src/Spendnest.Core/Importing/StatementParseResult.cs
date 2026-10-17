namespace Spendnest.Core.Importing;

/// <summary>
/// Contains parsed statement rows, warnings, aggregate parse counts, and the card number from the first data row and the recognized card issuer when the statement has them.
/// </summary>
public sealed record StatementParseResult(
    IReadOnlyList<ParsedStatementRow> Rows,
    IReadOnlyList<StatementParseWarning> Warnings,
    int TotalRowCount,
    int FailedRowCount,
    string? Error = null,
    string? CardNumber = null,
    string? CardIssuer = null)
{
    public bool HasTransaction => Rows.Count > 0;
}
