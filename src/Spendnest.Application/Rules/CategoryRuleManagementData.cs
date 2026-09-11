using Spendnest.Core.Accounts;
using Spendnest.Core.Categories;

namespace Spendnest.Application.Rules;

public sealed record CategoryRuleManagementData(
    IReadOnlyList<ManagedCategoryRule> Rules,
    IReadOnlyList<BuiltInCategory> Categories,
    IReadOnlyList<CardAccount> Cards,
    Guid? SelectedRuleId,
    IReadOnlyList<ManagedRuleTransactionPreview> PreviewRows);
