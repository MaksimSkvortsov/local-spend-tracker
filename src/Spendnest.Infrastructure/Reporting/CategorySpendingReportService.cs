using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Reporting;
using Spendnest.Core.Transactions;

namespace Spendnest.Infrastructure.Reporting;

/// <summary>
/// Builds category spending reports from the current transaction repository.
/// </summary>
public sealed class CategorySpendingReportService : ICategorySpendingReportService
{
    private readonly ITransactionRepository transactionRepository;
    private readonly ITransactionCategoryAssignmentRepository assignmentRepository;

    public CategorySpendingReportService(
        ITransactionRepository transactionRepository,
        ITransactionCategoryAssignmentRepository assignmentRepository)
    {
        this.transactionRepository = transactionRepository;
        this.assignmentRepository = assignmentRepository;
    }

    public async Task<CategorySpendingReport> BuildAsync(CancellationToken cancellationToken)
    {
        return await BuildAsync(new TransactionQuery(), cancellationToken).ConfigureAwait(false);
    }

    public async Task<CategorySpendingReport> BuildAsync(
        TransactionQuery query,
        CancellationToken cancellationToken)
    {
        var transactions = await transactionRepository.ListAsync(query, cancellationToken).ConfigureAwait(false);
        var assignmentsByTransactionId = (await assignmentRepository.ListAsync(cancellationToken).ConfigureAwait(false))
            .ToDictionary(assignment => assignment.TransactionId);
        var categoryNamesById = BuiltInCategories.All.ToDictionary(category => category.Id, category => category.Name);

        var lines = transactions
            .GroupBy(transaction => assignmentsByTransactionId.GetValueOrDefault(transaction.Id)?.CategoryId
                ?? BuiltInCategoryIds.Other)
            .Select(group => new CategorySpendingReportLine(
                group.Key,
                categoryNamesById.GetValueOrDefault(group.Key, group.Key.ToString()),
                group.Count(),
                group.Sum(transaction => transaction.Amount)))
            .OrderByDescending(line => Math.Abs(line.Amount))
            .ThenBy(line => line.CategoryName)
            .ToArray();

        return new CategorySpendingReport(
            lines,
            lines.Sum(line => line.Amount));
    }
}
