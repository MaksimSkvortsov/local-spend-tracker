namespace Spendnest.Desktop.Presentation.Transactions;

public static class TransactionFilters
{
    public static IReadOnlyList<TransactionRow> Apply(
        IReadOnlyList<TransactionRow> transactions,
        TransactionFilter filter)
    {
        return transactions
            .Where(transaction => MatchesSelectedCard(transaction, filter.SelectedCardId))
            .Where(transaction => MatchesSelectedCategory(transaction, filter.SelectedCategoryId))
            .Where(transaction => MatchesReviewFilter(transaction, filter.ReviewFilterMode))
            .ToArray();
    }

    private static bool MatchesSelectedCard(TransactionRow transaction, string selectedCardId)
    {
        if (string.IsNullOrWhiteSpace(selectedCardId))
        {
            return true;
        }

        return Guid.TryParse(selectedCardId, out var cardId)
               && transaction.CardAccountId == cardId;
    }

    private static bool MatchesSelectedCategory(TransactionRow transaction, string selectedCategoryId)
    {
        if (string.IsNullOrWhiteSpace(selectedCategoryId))
        {
            return true;
        }

        return int.TryParse(selectedCategoryId, out var categoryId)
               && transaction.CategoryId == categoryId;
    }

    private static bool MatchesReviewFilter(TransactionRow transaction, ReviewFilterMode reviewFilterMode)
    {
        return reviewFilterMode == ReviewFilterMode.All
               || transaction.NeedsReview;
    }
}
