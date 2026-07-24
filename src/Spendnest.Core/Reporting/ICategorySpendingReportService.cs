using Spendnest.Core.Transactions;

namespace Spendnest.Core.Reporting;

/// <summary>
/// Builds category spending reports from stored transactions.
/// </summary>
public interface ICategorySpendingReportService
{
    Task<CategorySpendingReport> BuildAsync(CancellationToken cancellationToken);

    Task<CategorySpendingReport> BuildAsync(
        TransactionQuery query,
        CancellationToken cancellationToken);
}
