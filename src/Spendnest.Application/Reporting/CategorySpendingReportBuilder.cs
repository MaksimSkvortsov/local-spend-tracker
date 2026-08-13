using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Reporting;
using Spendnest.Core.Transactions;

namespace Spendnest.Application.Reporting;

public sealed class CategorySpendingReportBuilder
{
    public CategorySpendingReport Build(
        IReadOnlyList<Transaction> transactions,
        IReadOnlyList<TransactionCategoryAssignment> assignments,
        IReadOnlyList<BuiltInCategory> categories)
    {
        ArgumentNullException.ThrowIfNull(transactions);
        ArgumentNullException.ThrowIfNull(assignments);
        ArgumentNullException.ThrowIfNull(categories);

        var assignmentsByTransactionId = assignments.ToDictionary(assignment => assignment.TransactionId);
        var categoryNamesById = categories.ToDictionary(category => category.Id, category => category.Name);
        var categorizedTransactions = CategorizeTransactions(transactions, assignmentsByTransactionId);
        var lines = BuildReportLines(categorizedTransactions, categoryNamesById);

        return new CategorySpendingReport(
            lines,
            CalculateTotalSpending(lines));
    }

    private static IReadOnlyList<CategorizedTransaction> CategorizeTransactions(
        IReadOnlyList<Transaction> transactions,
        IReadOnlyDictionary<Guid, TransactionCategoryAssignment> assignmentsByTransactionId)
    {
        return transactions
            .Select(transaction => new CategorizedTransaction(
                transaction,
                ResolveCategoryId(transaction, assignmentsByTransactionId)))
            .Where(item => item.CategoryId != BuiltInCategoryIds.CreditCardPayment)
            .ToArray();
    }

    private static int ResolveCategoryId(
        Transaction transaction,
        IReadOnlyDictionary<Guid, TransactionCategoryAssignment> assignmentsByTransactionId)
    {
        return assignmentsByTransactionId.GetValueOrDefault(transaction.Id)?.CategoryId
            ?? BuiltInCategoryIds.Other;
    }

    private static IReadOnlyList<CategorySpendingReportLine> BuildReportLines(
        IReadOnlyList<CategorizedTransaction> categorizedTransactions,
        IReadOnlyDictionary<int, string> categoryNamesById)
    {
        return categorizedTransactions
            .GroupBy(item => item.CategoryId)
            .Select(group => CreateReportLine(group, categoryNamesById))
            .OrderByDescending(line => Math.Abs(line.Amount))
            .ThenBy(line => line.CategoryName)
            .ToArray();
    }

    private static CategorySpendingReportLine CreateReportLine(
        IGrouping<int, CategorizedTransaction> group,
        IReadOnlyDictionary<int, string> categoryNamesById)
    {
        return new CategorySpendingReportLine(
            group.Key,
            ResolveCategoryName(group.Key, categoryNamesById),
            group.Count(),
            group.Sum(item => item.Transaction.Amount));
    }

    private static string ResolveCategoryName(
        int categoryId,
        IReadOnlyDictionary<int, string> categoryNamesById)
    {
        return categoryNamesById.GetValueOrDefault(categoryId, categoryId.ToString());
    }

    private static decimal CalculateTotalSpending(IReadOnlyList<CategorySpendingReportLine> lines)
    {
        return lines.Sum(line => line.Amount);
    }

    private sealed record CategorizedTransaction(
        Transaction Transaction,
        int CategoryId);
}
