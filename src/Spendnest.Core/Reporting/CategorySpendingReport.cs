namespace Spendnest.Core.Reporting;

/// <summary>
/// Represents spending grouped by category across a transaction set.
/// </summary>
public sealed record CategorySpendingReport(
    IReadOnlyList<CategorySpendingReportLine> Lines,
    decimal TotalSpending);
