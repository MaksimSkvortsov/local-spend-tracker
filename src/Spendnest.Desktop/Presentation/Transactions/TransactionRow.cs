namespace Spendnest.Desktop.Presentation.Transactions;

public sealed record TransactionRow(
    Guid Id,
    DateOnly PostedDate,
    string Description,
    int CategoryId,
    string CategoryColor,
    string CardName,
    decimal Amount,
    bool NeedsReview);
