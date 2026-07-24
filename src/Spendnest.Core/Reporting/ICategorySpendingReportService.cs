namespace Spendnest.Core.Reporting;

/// <summary>
/// Builds category spending reports from stored transactions.
/// </summary>
public interface ICategorySpendingReportService
{
    Task<CategorySpendingReport> BuildAsync(CancellationToken cancellationToken);
}
