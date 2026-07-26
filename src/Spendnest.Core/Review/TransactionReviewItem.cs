using Spendnest.Core.Categorization;

namespace Spendnest.Core.Review;

/// <summary>
/// Represents a transaction that needs user review.
/// </summary>
public sealed record TransactionReviewItem(
    Guid TransactionId,
    DateOnly PostedDate,
    string Description,
    decimal Amount,
    int? CategoryId,
    CategorizationSource? Source,
    decimal? Confidence,
    string Explanation);
