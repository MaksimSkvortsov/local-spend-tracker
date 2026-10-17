using Spendnest.Core.Categorization;

namespace Spendnest.Application.Rules;

public sealed record CategoryRuleCreateResult(
    CategoryRule Rule,
    int AppliedCount,
    IReadOnlyList<CategoryRuleOverride> Overrides);

/// <summary>
/// An existing higher-priority rule that keeps transactions the new rule also matches.
/// </summary>
public sealed record CategoryRuleOverride(
    string Pattern,
    CategoryRuleMatchType MatchType,
    int TransactionCount);
