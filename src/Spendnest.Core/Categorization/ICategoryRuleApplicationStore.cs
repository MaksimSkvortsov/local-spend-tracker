namespace Spendnest.Core.Categorization;

/// <summary>
/// Persists applying a category rule and its affected transaction assignments together.
/// </summary>
public interface ICategoryRuleApplicationStore
{
    Task UpdateCategoryAndAssignmentsAsync(
        Guid ruleId,
        string pattern,
        CategoryRuleMatchType matchType,
        int categoryId,
        IReadOnlyList<TransactionCategoryAssignment> assignments,
        CancellationToken cancellationToken);
}
