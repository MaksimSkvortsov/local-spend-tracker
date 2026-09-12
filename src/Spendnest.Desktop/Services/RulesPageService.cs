using Spendnest.Core.Accounts;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Desktop.Presentation.Rules;
using Spendnest.Application.Rules;

namespace Spendnest.Desktop.Services;

public sealed class RulesPageService(
    CategoryRuleManagementService ruleManagementService)
{
    public async Task<RulesPageData> LoadAsync(
        Guid? selectedRuleId,
        CategoryRuleUpdate? draftUpdate,
        CancellationToken cancellationToken)
    {
        var pageData = await ruleManagementService.LoadAsync(selectedRuleId, draftUpdate, cancellationToken);
        var categoryLookup = pageData.Categories.ToDictionary(category => category.Id);
        var cardNamesById = pageData.Cards.ToDictionary(card => card.Id, card => card.Name);

        return new RulesPageData(
            pageData.Rules
                .Select(rule => ToRuleRow(rule, categoryLookup))
                .ToArray(),
            pageData.Categories,
            pageData.SelectedRuleId,
            pageData.PreviewRows
                .Select(row => ToPreviewRow(row, cardNamesById, categoryLookup))
                .ToArray());
    }

    public Task<RulesPageData> LoadAsync(
        Guid? selectedRuleId,
        CancellationToken cancellationToken)
    {
        return LoadAsync(selectedRuleId, null, cancellationToken);
    }

    public async Task<int> SaveAndApplyAsync(
        CategoryRuleUpdate update,
        CancellationToken cancellationToken)
    {
        return await ruleManagementService.SaveAndApplyAsync(update, cancellationToken);
    }

    public async Task<RulesPageData> LoadCreateDraftAsync(
        CategoryRuleCreate draft,
        CancellationToken cancellationToken)
    {
        var pageData = await ruleManagementService.LoadCreateDraftAsync(draft, cancellationToken);
        var categoryLookup = pageData.Categories.ToDictionary(category => category.Id);
        var cardNamesById = pageData.Cards.ToDictionary(card => card.Id, card => card.Name);

        return new RulesPageData(
            pageData.Rules
                .Select(rule => ToRuleRow(rule, categoryLookup))
                .ToArray(),
            pageData.Categories,
            pageData.SelectedRuleId,
            pageData.PreviewRows
                .Select(row => ToPreviewRow(row, cardNamesById, categoryLookup))
                .ToArray());
    }

    public async Task<CategoryRuleCreateResult> CreateAndApplyAsync(
        CategoryRuleCreate create,
        CancellationToken cancellationToken)
    {
        return await ruleManagementService.CreateAndApplyAsync(create, cancellationToken);
    }

    private static CategoryRuleRow ToRuleRow(
        ManagedCategoryRule rule,
        IReadOnlyDictionary<int, BuiltInCategory> categoriesById)
    {
        var category = categoriesById.GetValueOrDefault(rule.CategoryId);

        return new CategoryRuleRow(
            rule.Id,
            rule.Pattern,
            rule.MatchType,
            rule.CategoryId,
            category?.Name ?? "Unknown",
            category?.ColorHex ?? "#e5e7eb",
            rule.MatchCount);
    }

    private static RuleTransactionPreviewRow ToPreviewRow(
        ManagedRuleTransactionPreview row,
        IReadOnlyDictionary<Guid, string> cardNamesById,
        IReadOnlyDictionary<int, BuiltInCategory> categoriesById)
    {
        var currentCategory = categoriesById.GetValueOrDefault(row.CurrentCategoryId);
        var newCategory = categoriesById.GetValueOrDefault(row.NewCategoryId);

        return new RuleTransactionPreviewRow(
            row.TransactionId,
            row.PostedDate,
            row.Description,
            cardNamesById.GetValueOrDefault(row.CardAccountId, "Unknown Card"),
            row.Amount,
            row.CurrentCategoryId,
            currentCategory?.Name ?? "Other",
            currentCategory?.ColorHex ?? "#e5e7eb",
            row.NewCategoryId,
            newCategory?.Name ?? "Unknown",
            newCategory?.ColorHex ?? "#e5e7eb",
            row.RuleWins);
    }
}
