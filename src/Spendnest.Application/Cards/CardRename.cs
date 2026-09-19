namespace Spendnest.Application.Cards;

public sealed record CardRename(
    Guid CardAccountId,
    string Name);
