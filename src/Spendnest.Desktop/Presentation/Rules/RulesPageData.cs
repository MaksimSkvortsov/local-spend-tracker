using Spendnest.Core.Categories;

namespace Spendnest.Desktop.Presentation.Rules;

public sealed record RulesPageData(
    IReadOnlyList<CategoryRuleRow> Rules,
    IReadOnlyList<BuiltInCategory> Categories,
    Guid? SelectedRuleId,
    IReadOnlyList<RuleTransactionPreviewRow> PreviewRows)
{
    public static RulesPageData Empty { get; } = new([], [], null, []);
}
