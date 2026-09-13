using Spendnest.Application.Rules;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;

namespace Spendnest.Desktop.Presentation.Rules;

public sealed class RuleEditorDraft
{
    public CategoryRuleRow? Baseline { get; private set; }

    public string Pattern { get; set; } = string.Empty;

    public CategoryRuleMatchType MatchType { get; set; } = CategoryRuleMatchType.Contains;

    public string SelectedCategoryId { get; set; } = string.Empty;

    public bool IsCreating { get; private set; }

    public bool HasAttemptedSubmit { get; set; }

    public int? SelectedCategoryIdValue =>
        int.TryParse(SelectedCategoryId, out var categoryId)
            ? categoryId
            : null;

    public bool HasRuleChange =>
        HasCategoryChange || HasRuleShapeChange;

    public bool ShowPatternValidation =>
        HasAttemptedSubmit && string.IsNullOrWhiteSpace(Pattern);

    public string DraftListPattern =>
        string.IsNullOrWhiteSpace(Pattern) ? "NEW RULE" : Pattern.Trim();

    public string DetailTitle =>
        IsCreating ? "New category rule" : "Rule Details";

    public string DetailModeLabel =>
        IsCreating ? "Unsaved Draft" : "Saved Rule";

    public string DetailModeClass =>
        IsCreating ? "rule-mode-badge draft" : "rule-mode-badge";

    public bool CanSave(bool isSaving, bool isRuleLoading)
    {
        return !isSaving
            && !isRuleLoading
            && HasRuleChange
            && !string.IsNullOrWhiteSpace(Pattern);
    }

    public bool CanCreate(
        bool isSaving,
        bool isRuleLoading,
        IReadOnlyList<CategoryRuleRow> rules)
    {
        return !isSaving
            && !isRuleLoading
            && IsCreating
            && !string.IsNullOrWhiteSpace(Pattern)
            && SelectedCategoryIdValue is not null
            && !DuplicateRuleExists(rules);
    }

    public bool CanSubmit(
        bool isSaving,
        bool isRuleLoading,
        IReadOnlyList<CategoryRuleRow> rules)
    {
        return IsCreating
            ? CanCreate(isSaving, isRuleLoading, rules)
            : CanSave(isSaving, isRuleLoading);
    }

    public bool DuplicateRuleExists(IReadOnlyList<CategoryRuleRow> rules)
    {
        return IsCreating
            && !string.IsNullOrWhiteSpace(Pattern)
            && rules.Any(rule =>
                rule.MatchType == MatchType
                && string.Equals(
                    NormalizeRulePattern(rule.Pattern),
                    NormalizeRulePattern(Pattern),
                    StringComparison.Ordinal));
    }

    public string DirtyStateLabel()
    {
        return IsCreating
            ? "Unsaved draft"
            : HasRuleChange ? "Rule modified" : "No changes";
    }

    public string PrimaryActionLabel(int wouldChangeCount)
    {
        return IsCreating
            ? wouldChangeCount == 0
                ? "Create Rule"
                : $"Create & Apply to {wouldChangeCount} Transactions"
            : "Save & Apply to All";
    }

    public string SelectedCategoryName(IReadOnlyList<BuiltInCategory> categories)
    {
        return SelectedCategoryIdValue is int categoryId
            ? categories.FirstOrDefault(category => category.Id == categoryId)?.Name ?? "selected category"
            : "selected category";
    }

    public string SelectedCategoryColorHex(IReadOnlyList<BuiltInCategory> categories)
    {
        return SelectedCategoryIdValue is int categoryId
            ? categories.FirstOrDefault(category => category.Id == categoryId)?.ColorHex ?? "#e5e7eb"
            : "#e5e7eb";
    }

    public void ResetFromSelectedRule(CategoryRuleRow? selectedRule)
    {
        Baseline = selectedRule;
        IsCreating = false;
        HasAttemptedSubmit = false;
        Pattern = selectedRule?.Pattern ?? string.Empty;
        MatchType = selectedRule?.MatchType ?? CategoryRuleMatchType.Contains;
        SelectedCategoryId = selectedRule?.CategoryId.ToString() ?? string.Empty;
    }

    public void StartCreate(IReadOnlyList<BuiltInCategory> categories)
    {
        IsCreating = true;
        Baseline = null;
        HasAttemptedSubmit = false;
        Pattern = string.Empty;
        MatchType = CategoryRuleMatchType.Contains;
        SelectedCategoryId = categories.FirstOrDefault()?.Id.ToString() ?? string.Empty;
    }

    public void CancelCreate()
    {
        IsCreating = false;
        HasAttemptedSubmit = false;
    }

    public CategoryRuleUpdate BuildUpdate()
    {
        return new CategoryRuleUpdate(
            Baseline?.Id ?? Guid.Empty,
            Pattern,
            MatchType,
            SelectedCategoryIdValue ?? 0);
    }

    public CategoryRuleCreate BuildCreate()
    {
        return new CategoryRuleCreate(
            Pattern,
            MatchType,
            SelectedCategoryIdValue ?? 0);
    }

    private bool HasCategoryChange =>
        Baseline is not null
        && SelectedCategoryIdValue is int categoryId
        && categoryId != Baseline.CategoryId;

    private bool HasRuleShapeChange =>
        Baseline is not null
        && (!string.Equals(Pattern.Trim(), Baseline.Pattern, StringComparison.Ordinal)
            || MatchType != Baseline.MatchType);

    private static string NormalizeRulePattern(string value)
    {
        return value.Trim().ToUpperInvariant();
    }
}
