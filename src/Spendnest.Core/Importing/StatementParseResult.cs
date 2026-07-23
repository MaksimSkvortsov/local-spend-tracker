namespace Spendnest.Core.Importing;

/// <summary>
/// Contains parsed statement rows, warnings, and aggregate parse counts.
/// </summary>
public sealed record StatementParseResult(
    IReadOnlyList<ParsedStatementRow> Rows,
    IReadOnlyList<StatementParseWarning> Warnings,
    int TotalRowCount,
    int FailedRowCount);
