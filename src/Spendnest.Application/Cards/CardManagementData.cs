namespace Spendnest.Application.Cards;

public sealed record CardManagementData(
    IReadOnlyList<CardSummary> Cards,
    int TotalTransactionCount);
