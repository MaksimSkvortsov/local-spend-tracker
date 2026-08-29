using Spendnest.Core.Accounts;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Credentials;
using Spendnest.Core.Reporting;
using Spendnest.Core.Review;
using Spendnest.Core.Transactions;
using Spendnest.Desktop.Presentation.Dashboard;

namespace Spendnest.Desktop.Services;

public sealed class DashboardService(
    ITransactionRepository transactionRepository,
    ICategorySpendingReportService reportService,
    ITransactionCategoryAssignmentRepository assignmentRepository,
    ITransactionReviewService reviewService,
    ICardAccountRepository cardAccountRepository,
    ICategoryRepository categoryRepository,
    ICredentialStore credentialStore)
{
    public async Task<DashboardModel> LoadAsync(
        DashboardLoadRequest request,
        CancellationToken cancellationToken)
    {
        var isAiConfigured = await IsAiConfiguredAsync(cancellationToken);
        var transactions = await transactionRepository.ListAsync(cancellationToken);
        var fallbackDate = transactions
            .OrderByDescending(transaction => transaction.PostedDate)
            .Select(transaction => (DateOnly?)transaction.PostedDate)
            .FirstOrDefault();
        var selectedDate = request.FocusDate ?? fallbackDate;
        var availableYears = GetAvailableYears(transactions);
        var mode = request.PreserveSelectedWindow
            ? request.CurrentMode
            : ParseReportMode(request.StoredMode);
        var year = GetSelectedYear(
            availableYears,
            request.PreserveSelectedWindow ? request.CurrentYear : request.StoredYear,
            selectedDate?.Year ?? 0);
        var month = GetSelectedMonth(
            request.PreserveSelectedWindow ? request.CurrentMonth : request.StoredMonth,
            selectedDate?.Month ?? 0);

        if (selectedDate is null)
        {
            return DashboardModel.Empty with
            {
                Transactions = transactions,
                Mode = mode,
                Year = year,
                Month = month,
                AvailableYears = availableYears,
                IsAiConfigured = isAiConfigured
            };
        }

        var cards = await cardAccountRepository.ListAsync(cancellationToken);
        var categories = await categoryRepository.ListAsync(cancellationToken);
        var categoryNamesById = categories.ToDictionary(category => category.Id, category => category.Name);
        var categoryColorsById = categories.ToDictionary(category => category.Id, category => category.ColorHex);
        var cardNamesById = cards.ToDictionary(card => card.Id, card => card.Name);
        var assignmentsByTransactionId = (await assignmentRepository.ListAsync(cancellationToken))
            .ToDictionary(assignment => assignment.TransactionId);
        var reviewCount = await reviewService.CountNeedsReviewAsync(cancellationToken);

        var query = BuildQuery(mode, year, month);
        var filteredTransactions = await transactionRepository.ListAsync(query, cancellationToken);
        var report = await reportService.BuildAsync(query, cancellationToken);
        var totalSpending = report.TotalSpending;
        var windowRangeLabel = GetWindowRangeLabel(query, transactions);

        return new DashboardModel(
            transactions,
            filteredTransactions,
            report,
            totalSpending,
            reviewCount,
            mode,
            year,
            month,
            availableYears,
            cards,
            categoryColorsById,
            cardNamesById,
            BuildBiggestTransactionRows(
                filteredTransactions,
                assignmentsByTransactionId,
                categoryNamesById,
                categoryColorsById),
            isAiConfigured,
            GetReportWindowShortLabel(mode, year, month),
            windowRangeLabel);
    }

    private async Task<bool> IsAiConfiguredAsync(CancellationToken cancellationToken)
    {
        var apiKey = await credentialStore.GetStringAsync(CredentialKeys.OpenAiApiKey, cancellationToken);
        return !string.IsNullOrWhiteSpace(apiKey);
    }

    private static TransactionQuery BuildQuery(
        ReportMode mode,
        int year,
        int month)
    {
        if (mode == ReportMode.All)
        {
            return new TransactionQuery();
        }

        if (mode == ReportMode.Year)
        {
            return new TransactionQuery
            {
                StartDate = new DateOnly(year, 1, 1),
                EndDate = new DateOnly(year, 12, 31)
            };
        }

        var monthStart = new DateOnly(year, month, 1);
        return new TransactionQuery
        {
            StartDate = monthStart,
            EndDate = monthStart.AddMonths(1).AddDays(-1)
        };
    }

    private static IReadOnlyList<BiggestTransactionRow> BuildBiggestTransactionRows(
        IReadOnlyList<Transaction> filteredTransactions,
        IReadOnlyDictionary<Guid, TransactionCategoryAssignment> assignmentsByTransactionId,
        IReadOnlyDictionary<int, string> categoryNamesById,
        IReadOnlyDictionary<int, string> categoryColorsById)
    {
        return filteredTransactions
            .Where(transaction => transaction.Amount > 0)
            .OrderByDescending(transaction => transaction.Amount)
            .ThenByDescending(transaction => transaction.ImportedAtUtc)
            .Take(5)
            .Select(transaction =>
            {
                var categoryId = assignmentsByTransactionId.TryGetValue(transaction.Id, out var assignment)
                    ? assignment.CategoryId
                    : BuiltInCategoryIds.Other;

                return new BiggestTransactionRow(
                    transaction.OriginalDescription,
                    categoryNamesById.GetValueOrDefault(categoryId, "Other"),
                    categoryColorsById.GetValueOrDefault(categoryId, "#e5e7e2"),
                    transaction.Amount);
            })
            .ToArray();
    }

    private static IReadOnlyList<int> GetAvailableYears(IReadOnlyList<Transaction> transactions)
    {
        return transactions
            .Select(transaction => transaction.PostedDate.Year)
            .Distinct()
            .OrderByDescending(year => year)
            .ToArray();
    }

    private static string GetReportWindowShortLabel(
        ReportMode mode,
        int year,
        int month)
    {
        return mode == ReportMode.All
            ? "All time"
            : mode == ReportMode.Year
            ? year.ToString()
            : new DateOnly(year, month, 1).ToString("yyyy-MM");
    }

    private static string GetWindowRangeLabel(
        TransactionQuery query,
        IReadOnlyList<Transaction> transactions)
    {
        if (query.StartDate is null || query.EndDate is null)
        {
            var firstTransactionDate = transactions
                .Select(transaction => (DateOnly?)transaction.PostedDate)
                .Min();
            var lastTransactionDate = transactions
                .Select(transaction => (DateOnly?)transaction.PostedDate)
                .Max();

            return firstTransactionDate is null || lastTransactionDate is null
                ? string.Empty
                : $"{firstTransactionDate:MMM d, yyyy} - {lastTransactionDate:MMM d, yyyy}";
        }

        return $"{query.StartDate:MMM d} - {query.EndDate:MMM d, yyyy}";
    }

    private static ReportMode ParseReportMode(string? value)
    {
        return Enum.TryParse<ReportMode>(value, ignoreCase: true, out var mode)
            ? mode
            : ReportMode.Year;
    }

    private static int GetSelectedYear(
        IReadOnlyList<int> availableYears,
        int? preferredYear,
        int fallbackYear)
    {
        if (preferredYear is not null && availableYears.Contains(preferredYear.Value))
        {
            return preferredYear.Value;
        }

        return availableYears.Contains(fallbackYear)
            ? fallbackYear
            : availableYears.FirstOrDefault();
    }

    private static int GetSelectedMonth(
        int? preferredMonth,
        int fallbackMonth)
    {
        return preferredMonth is >= 1 and <= 12
            ? preferredMonth.Value
            : fallbackMonth;
    }
}
