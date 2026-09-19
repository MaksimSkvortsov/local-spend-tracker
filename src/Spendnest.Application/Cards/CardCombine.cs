namespace Spendnest.Application.Cards;

public sealed record CardCombine(
    Guid SourceCardAccountId,
    Guid TargetCardAccountId);
