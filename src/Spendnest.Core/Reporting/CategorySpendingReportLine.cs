namespace Spendnest.Core.Reporting;

/// <summary>
/// Represents one category row in a spending report.
/// </summary>
public sealed record CategorySpendingReportLine(
    int CategoryId,
    string CategoryName,
    int TransactionCount,
    decimal Amount);
