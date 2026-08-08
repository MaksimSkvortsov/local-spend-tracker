namespace Spendnest.Core.Categorization;

/// <summary>
/// Represents a category result produced for one transaction.
/// </summary>
public sealed record TransactionCategorization(
    Guid TransactionId,
    int CategoryId,
    decimal Confidence,
    bool NeedsReview,
    CategorizationSource Source,
    string Explanation,
    string? LearnedRulePrefix = null);
