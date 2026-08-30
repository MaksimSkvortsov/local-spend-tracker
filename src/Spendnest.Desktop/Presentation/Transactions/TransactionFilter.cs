namespace Spendnest.Desktop.Presentation.Transactions;

public sealed record TransactionFilter(
    string SelectedCardId,
    string SelectedCategoryId,
    ReviewFilterMode ReviewFilterMode);
