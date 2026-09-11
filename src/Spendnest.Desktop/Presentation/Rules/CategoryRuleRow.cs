using Spendnest.Core.Categorization;

namespace Spendnest.Desktop.Presentation.Rules;

public sealed record CategoryRuleRow(
    Guid Id,
    string Pattern,
    CategoryRuleMatchType MatchType,
    int CategoryId,
    string CategoryName,
    string CategoryColorHex,
    int MatchCount);
