namespace Spendnest.Core.Categorization;

/// <summary>
/// Represents a category decision for one transaction.
/// </summary>
public sealed record TransactionCategorization(
    Guid TransactionId,
    string CategoryCode,
    decimal Confidence,
    bool NeedsReview,
    CategorizationSource Source,
    string Explanation);
