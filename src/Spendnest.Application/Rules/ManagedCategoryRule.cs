using Spendnest.Core.Categorization;

namespace Spendnest.Application.Rules;

public sealed record ManagedCategoryRule(
    Guid Id,
    string Pattern,
    CategoryRuleMatchType MatchType,
    int CategoryId,
    int MatchCount);
