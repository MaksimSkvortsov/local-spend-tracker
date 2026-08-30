namespace Spendnest.Desktop.Presentation.Transactions;

public sealed record TransactionRow(
    Guid Id,
    Guid CardAccountId,
    DateOnly PostedDate,
    string Description,
    int CategoryId,
    string CategoryColor,
    string CardName,
    decimal Amount,
    bool NeedsReview);
