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
    private readonly ITransactionCategoryMapper categoryMapper;

    public CategorySpendingReportService(
        ITransactionRepository transactionRepository,
        ITransactionCategoryMapper categoryMapper)
    {
        this.transactionRepository = transactionRepository;
        this.categoryMapper = categoryMapper;
    }

    public async Task<CategorySpendingReport> BuildAsync(CancellationToken cancellationToken)
    {
        var transactions = await transactionRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        var categoryNamesByCode = BuiltInCategories.All.ToDictionary(category => category.Code, category => category.Name);

        var lines = transactions
            .GroupBy(categoryMapper.MapCategoryCode)
            .Select(group => new CategorySpendingReportLine(
                group.Key,
                categoryNamesByCode.GetValueOrDefault(group.Key, group.Key),
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
