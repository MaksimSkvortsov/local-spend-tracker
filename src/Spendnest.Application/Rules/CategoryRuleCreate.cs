using Spendnest.Core.Categorization;

namespace Spendnest.Application.Rules;

public sealed record CategoryRuleCreate(
    string Pattern,
    CategoryRuleMatchType MatchType,
    int CategoryId);
