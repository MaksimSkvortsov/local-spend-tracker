using Spendnest.Core.Categorization;

namespace Spendnest.Desktop.Presentation.Importing;

public sealed record CategorizationHistorySummary(
    int CategorizedTransactionCount,
    int NeedsReviewCount)
{
    public static CategorizationHistorySummary From(IReadOnlyList<TransactionCategorization> categorizations)
    {
        return new CategorizationHistorySummary(
            categorizations.Count,
            categorizations.Count(categorization => categorization.NeedsReview));
    }
}
