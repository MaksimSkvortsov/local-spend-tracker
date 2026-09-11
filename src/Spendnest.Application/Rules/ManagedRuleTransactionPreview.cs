namespace Spendnest.Application.Rules;

public sealed record ManagedRuleTransactionPreview(
    Guid TransactionId,
    DateOnly PostedDate,
    string Description,
    Guid CardAccountId,
    decimal Amount,
    int CurrentCategoryId,
    int NewCategoryId);
