namespace Spendnest.Application.Cards;

public sealed record CardSummary(
    Guid Id,
    string Name,
    int TransactionCount,
    int ImportCount,
    DateOnly? LastImportedDate,
    bool HasPossibleDuplicateName);
