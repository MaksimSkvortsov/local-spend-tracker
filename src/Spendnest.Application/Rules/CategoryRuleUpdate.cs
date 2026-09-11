using Spendnest.Core.Categorization;

namespace Spendnest.Application.Rules;

public sealed record CategoryRuleUpdate(
    Guid RuleId,
    string Pattern,
    CategoryRuleMatchType MatchType,
    int CategoryId);
