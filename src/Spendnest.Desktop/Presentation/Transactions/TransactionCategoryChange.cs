namespace Spendnest.Desktop.Presentation.Transactions;

public sealed record TransactionCategoryChange(
    Guid TransactionId,
    int CurrentCategoryId,
    string? CategoryId);
