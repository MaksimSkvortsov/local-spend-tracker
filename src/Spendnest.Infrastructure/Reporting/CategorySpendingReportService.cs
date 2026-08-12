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
    private readonly ICategoryRepository categoryRepository;

    public CategorySpendingReportService(
        ITransactionRepository transactionRepository,
        ITransactionCategoryAssignmentRepository assignmentRepository,
        ICategoryRepository categoryRepository)
    {
        this.transactionRepository = transactionRepository;
        this.assignmentRepository = assignmentRepository;
        this.categoryRepository = categoryRepository;
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
        var categoryNamesById = (await categoryRepository.ListAsync(cancellationToken).ConfigureAwait(false))
            .ToDictionary(category => category.Id, category => category.Name);

        var categorizedTransactions = transactions
            .Select(transaction => new
            {
                Transaction = transaction,
                CategoryId = assignmentsByTransactionId.GetValueOrDefault(transaction.Id)?.CategoryId
                    ?? BuiltInCategoryIds.Other
            })
            .Where(item => item.CategoryId != BuiltInCategoryIds.CreditCardPayment)
            .ToArray();

        var lines = categorizedTransactions
            .GroupBy(item => item.CategoryId)
            .Select(group => new CategorySpendingReportLine(
                group.Key,
                categoryNamesById.GetValueOrDefault(group.Key, group.Key.ToString()),
                group.Count(),
                group.Sum(item => item.Transaction.Amount)))
            .OrderByDescending(line => Math.Abs(line.Amount))
            .ThenBy(line => line.CategoryName)
            .ToArray();

        return new CategorySpendingReport(
            lines,
            lines.Sum(line => line.Amount));
    }
}
