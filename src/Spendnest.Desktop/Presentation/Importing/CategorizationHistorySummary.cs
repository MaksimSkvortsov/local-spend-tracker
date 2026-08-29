using Spendnest.Core.Categorization;

namespace Spendnest.Desktop.Presentation.Importing;

public sealed record CategorizationHistorySummary(
    int CategorizedTransactionCount,
    int NeedsReviewCount,
    int TimeoutCount)
{
    public static CategorizationHistorySummary From(IReadOnlyList<TransactionCategorization> categorizations)
    {
        return new CategorizationHistorySummary(
            categorizations.Count,
            categorizations.Count(categorization => categorization.NeedsReview),
            categorizations.Count(categorization => categorization.Explanation.Contains("timed out", StringComparison.OrdinalIgnoreCase)));
    }
}
