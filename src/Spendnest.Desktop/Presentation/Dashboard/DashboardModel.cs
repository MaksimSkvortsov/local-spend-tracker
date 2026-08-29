using Spendnest.Core.Accounts;
using Spendnest.Core.Reporting;
using Spendnest.Core.Transactions;

namespace Spendnest.Desktop.Presentation.Dashboard;

public sealed record DashboardModel(
    IReadOnlyList<Transaction> Transactions,
    IReadOnlyList<Transaction> FilteredTransactions,
    CategorySpendingReport Report,
    decimal TotalSpending,
    int ReviewCount,
    ReportMode Mode,
    int Year,
    int Month,
    IReadOnlyList<int> AvailableYears,
    IReadOnlyList<CardAccount> Cards,
    IReadOnlyDictionary<int, string> CategoryColorsById,
    IReadOnlyDictionary<Guid, string> CardNamesById,
    IReadOnlyList<BiggestTransactionRow> BiggestTransactionRows,
    bool IsAiConfigured,
    string ReportWindowShortLabel,
    string WindowRangeLabel)
{
    public static DashboardModel Empty { get; } = new(
        [],
        [],
        new CategorySpendingReport([], 0m),
        0m,
        0,
        ReportMode.Year,
        0,
        0,
        [],
        [],
        new Dictionary<int, string>(),
        new Dictionary<Guid, string>(),
        [],
        false,
        string.Empty,
        string.Empty);
}
