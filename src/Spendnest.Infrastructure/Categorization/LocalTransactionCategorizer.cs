using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;

namespace Spendnest.Infrastructure.Categorization;

/// <summary>
/// Applies stored local rules before AI is used.
/// </summary>
public sealed class LocalTransactionCategorizer : ILocalTransactionCategorizer
{
    private readonly ICategoryRuleRepository ruleRepository;
    private readonly LocalCategoryRuleMatcher ruleMatcher;

    public LocalTransactionCategorizer(
        ICategoryRuleRepository ruleRepository,
        LocalCategoryRuleMatcher ruleMatcher)
    {
        this.ruleRepository = ruleRepository;
        this.ruleMatcher = ruleMatcher;
    }

    public async Task<IReadOnlyList<TransactionCategorization>> CategorizeKnownAsync(
        IReadOnlyList<Transaction> transactions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transactions);
        cancellationToken.ThrowIfCancellationRequested();

        var rules = await ruleRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        var results = new List<TransactionCategorization>();

        foreach (var transaction in transactions)
        {
            var rule = ruleMatcher.FindMatch(transaction, rules);

            if (rule is null)
            {
                continue;
            }

            results.Add(new TransactionCategorization(
                transaction.Id,
                rule.CategoryId,
                1m,
                false,
                CategorizationSource.LocalRules,
                "Matched local categorization knowledge."));
        }

        return results;
    }
}
