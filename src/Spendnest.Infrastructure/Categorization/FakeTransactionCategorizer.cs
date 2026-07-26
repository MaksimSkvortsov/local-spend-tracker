using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;

namespace Spendnest.Infrastructure.Categorization;

/// <summary>
/// Provides deterministic AI-shaped categorization for offline development and tests.
/// </summary>
public sealed class FakeTransactionCategorizer : ITransactionCategorizer
{
    private readonly ITransactionCategoryMapper categoryMapper;

    public FakeTransactionCategorizer(ITransactionCategoryMapper categoryMapper)
    {
        this.categoryMapper = categoryMapper;
    }

    public Task<IReadOnlyList<TransactionCategorization>> CategorizeAsync(
        IReadOnlyList<Transaction> transactions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transactions);
        cancellationToken.ThrowIfCancellationRequested();

        var categorizations = transactions
            .Select(transaction => new TransactionCategorization(
                transaction.Id,
                categoryMapper.MapCategoryId(transaction),
                1m,
                false,
                CategorizationSource.LocalAi,
                "Deterministic offline categorizer."))
            .ToArray();

        return Task.FromResult<IReadOnlyList<TransactionCategorization>>(categorizations);
    }
}
