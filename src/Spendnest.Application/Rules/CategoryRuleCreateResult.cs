using Spendnest.Core.Categorization;

namespace Spendnest.Application.Rules;

public sealed record CategoryRuleCreateResult(
    CategoryRule Rule,
    int AppliedCount);
