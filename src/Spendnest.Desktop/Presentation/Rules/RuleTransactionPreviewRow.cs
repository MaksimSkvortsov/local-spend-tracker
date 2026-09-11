namespace Spendnest.Desktop.Presentation.Rules;

public sealed record RuleTransactionPreviewRow(
    Guid TransactionId,
    DateOnly PostedDate,
    string Description,
    string CardName,
    decimal Amount,
    int CurrentCategoryId,
    string CurrentCategoryName,
    string CurrentCategoryColorHex,
    int NewCategoryId,
    string NewCategoryName,
    string NewCategoryColorHex);
