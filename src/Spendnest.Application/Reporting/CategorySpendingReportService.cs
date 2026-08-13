using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Reporting;
using Spendnest.Core.Transactions;

namespace Spendnest.Application.Reporting;

/// <summary>
/// Builds category spending reports from the current transaction repository.
/// </summary>
public sealed class CategorySpendingReportService : ICategorySpendingReportService
{
    private readonly ITransactionRepository transactionRepository;
    private readonly ITransactionCategoryAssignmentRepository assignmentRepository;
    private readonly ICategoryRepository categoryRepository;
    private readonly CategorySpendingReportBuilder reportBuilder;

    public CategorySpendingReportService(
        ITransactionRepository transactionRepository,
        ITransactionCategoryAssignmentRepository assignmentRepository,
        ICategoryRepository categoryRepository,
        CategorySpendingReportBuilder reportBuilder)
    {
        this.transactionRepository = transactionRepository;
        this.assignmentRepository = assignmentRepository;
        this.categoryRepository = categoryRepository;
        this.reportBuilder = reportBuilder;
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
        var assignments = await assignmentRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        var categories = await categoryRepository.ListAsync(cancellationToken).ConfigureAwait(false);

        return reportBuilder.Build(
            transactions,
            assignments,
            categories);
    }
}
