using Spendnest.Application.Categorization;
using Spendnest.Core.Accounts;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;

namespace Spendnest.Application.Rules;

public sealed class CategoryRuleManagementService(
    ICategoryRuleRepository ruleRepository,
    ICategoryRuleApplicationStore ruleApplicationStore,
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository,
    ITransactionCategoryAssignmentRepository assignmentRepository,
    ICardAccountRepository cardAccountRepository,
    LocalCategoryRuleMatcher ruleMatcher)
{
    public async Task<CategoryRuleManagementData> LoadAsync(
        Guid? selectedRuleId,
        CategoryRuleUpdate? draftUpdate,
        CancellationToken cancellationToken)
    {
        var categories = await categoryRepository.ListAsync(cancellationToken);
        var rules = await ruleRepository.ListAsync(cancellationToken);
        var selectedRule = SelectRule(rules, selectedRuleId);
        var effectiveRules = ApplyDraftRule(rules, selectedRule, draftUpdate);
        var transactions = await transactionRepository.ListAsync(cancellationToken);
        var assignments = await assignmentRepository.ListAsync(cancellationToken);
        var cards = await cardAccountRepository.ListAsync(cancellationToken);
        var matchingTransactionsByRuleId = BuildWinningTransactionsByRuleId(
            effectiveRules,
            transactions);
        var ruleRows = effectiveRules
            .Select(rule => new ManagedCategoryRule(
                rule.Id,
                rule.Pattern,
                rule.MatchType,
                rule.CategoryId,
                matchingTransactionsByRuleId.GetValueOrDefault(rule.Id)?.Count ?? 0))
            .ToArray();
        selectedRule = SelectRule(effectiveRules, selectedRule?.Id);
        var previewRows = selectedRule is null
            ? []
            : BuildPreviewRows(
                matchingTransactionsByRuleId.GetValueOrDefault(selectedRule.Id) ?? [],
                assignments,
                selectedRule.CategoryId);

        return new CategoryRuleManagementData(
            ruleRows,
            categories,
            cards,
            selectedRule?.Id,
            previewRows);
    }

    public Task<CategoryRuleManagementData> LoadAsync(
        Guid? selectedRuleId,
        CancellationToken cancellationToken)
    {
        return LoadAsync(selectedRuleId, null, cancellationToken);
    }

    public async Task<int> SaveAndApplyAsync(
        CategoryRuleUpdate update,
        CancellationToken cancellationToken)
    {
        ValidateUpdate(update);
        await ValidateCategoryAsync(update.CategoryId, cancellationToken);

        var rules = await ruleRepository.ListAsync(cancellationToken);
        var selectedRule = rules.FirstOrDefault(rule => rule.Id == update.RuleId)
            ?? throw new InvalidOperationException("Rule was not found.");
        var draftRule = new CategoryRule
        {
            Id = selectedRule.Id,
            Pattern = NormalizePattern(update.Pattern),
            MatchType = update.MatchType,
            CategoryId = update.CategoryId
        };
        var draftRules = rules
            .Select(rule => rule.Id == draftRule.Id ? draftRule : rule)
            .ToArray();
        var transactions = await transactionRepository.ListAsync(cancellationToken);
        var assignments = await assignmentRepository.ListAsync(cancellationToken);
        var assignmentsByTransactionId = assignments.ToDictionary(assignment => assignment.TransactionId);
        var matchingTransactions = FindWinningTransactions(draftRule, draftRules, transactions);
        var updatedAssignments = matchingTransactions
            .Select(transaction => CreateAppliedAssignment(
                transaction,
                assignmentsByTransactionId.GetValueOrDefault(transaction.Id),
                draftRule.Pattern,
                update.CategoryId))
            .ToArray();

        await ruleApplicationStore.UpdateCategoryAndAssignmentsAsync(
            update.RuleId,
            draftRule.Pattern,
            update.MatchType,
            update.CategoryId,
            updatedAssignments,
            cancellationToken);

        return updatedAssignments.Length;
    }

    public async Task<CategoryRuleManagementData> LoadCreateDraftAsync(
        CategoryRuleCreate draft,
        CancellationToken cancellationToken)
    {
        ValidateCreate(draft);

        var categories = await categoryRepository.ListAsync(cancellationToken);
        var rules = await ruleRepository.ListAsync(cancellationToken);
        var cards = await cardAccountRepository.ListAsync(cancellationToken);
        var draftRule = CreateDraftRule(draft);
        var transactions = await transactionRepository.ListAsync(cancellationToken);
        var assignments = await assignmentRepository.ListAsync(cancellationToken);
        var effectiveRules = rules.Concat([draftRule]).ToArray();
        var matchingTransactions = FindMatchingTransactions(draftRule, transactions);
        var winningTransactionIds = FindWinningTransactions(draftRule, effectiveRules, transactions)
            .Select(transaction => transaction.Id)
            .ToHashSet();
        var previewRows = BuildPreviewRows(
            matchingTransactions,
            assignments,
            draftRule.CategoryId,
            winningTransactionIds);
        var ruleRows = BuildRuleRows(rules, transactions);

        return new CategoryRuleManagementData(
            ruleRows,
            categories,
            cards,
            null,
            previewRows);
    }

    public async Task<CategoryRuleCreateResult> CreateAndApplyAsync(
        CategoryRuleCreate create,
        CancellationToken cancellationToken)
    {
        ValidateCreate(create);
        await ValidateCategoryAsync(create.CategoryId, cancellationToken);

        var normalizedPattern = NormalizePattern(create.Pattern);
        var rules = await ruleRepository.ListAsync(cancellationToken);
        ValidateDuplicateRule(rules, normalizedPattern, create.MatchType);

        var newRule = new CategoryRule
        {
            Pattern = normalizedPattern,
            MatchType = create.MatchType,
            CategoryId = create.CategoryId
        };
        var effectiveRules = rules.Concat([newRule]).ToArray();
        var transactions = await transactionRepository.ListAsync(cancellationToken);
        var assignments = await assignmentRepository.ListAsync(cancellationToken);
        var assignmentsByTransactionId = assignments.ToDictionary(assignment => assignment.TransactionId);
        var matchingTransactions = FindWinningTransactions(newRule, effectiveRules, transactions);
        var updatedAssignments = matchingTransactions
            .Select(transaction => CreateAppliedAssignment(
                transaction,
                assignmentsByTransactionId.GetValueOrDefault(transaction.Id),
                newRule.Pattern,
                create.CategoryId))
            .ToArray();

        await ruleApplicationStore.CreateRuleAndAssignmentsAsync(
            newRule,
            updatedAssignments,
            cancellationToken);

        return new CategoryRuleCreateResult(newRule, updatedAssignments.Length);
    }

    private static void ValidateUpdate(CategoryRuleUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);

        if (string.IsNullOrWhiteSpace(update.Pattern))
        {
            throw new InvalidOperationException("Rule pattern is required.");
        }

        if (!Enum.IsDefined(update.MatchType))
        {
            throw new InvalidOperationException("Selected match type is not supported.");
        }
    }

    private static void ValidateCreate(CategoryRuleCreate create)
    {
        ArgumentNullException.ThrowIfNull(create);

        if (string.IsNullOrWhiteSpace(create.Pattern))
        {
            throw new InvalidOperationException("Rule pattern is required.");
        }

        if (!Enum.IsDefined(create.MatchType))
        {
            throw new InvalidOperationException("Selected match type is not supported.");
        }
    }

    private static void ValidateDuplicateRule(
        IReadOnlyList<CategoryRule> rules,
        string pattern,
        CategoryRuleMatchType matchType)
    {
        var hasDuplicate = rules.Any(rule =>
            rule.MatchType == matchType
            && string.Equals(
                NormalizeRulePattern(rule.Pattern),
                NormalizeRulePattern(pattern),
                StringComparison.Ordinal));

        if (hasDuplicate)
        {
            throw new InvalidOperationException("A rule with this pattern and match type already exists.");
        }
    }

    private static string NormalizePattern(string pattern)
    {
        return pattern.Trim();
    }

    private static string NormalizeRulePattern(string pattern)
    {
        return NormalizePattern(pattern).ToUpperInvariant();
    }

    private static CategoryRule CreateDraftRule(CategoryRuleCreate draft)
    {
        return new CategoryRule
        {
            Pattern = NormalizePattern(draft.Pattern),
            MatchType = draft.MatchType,
            CategoryId = draft.CategoryId
        };
    }

    private async Task ValidateCategoryAsync(
        int categoryId,
        CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);
        if (category is null)
        {
            throw new InvalidOperationException("Selected category was not found.");
        }
    }

    private IReadOnlyList<Transaction> FindWinningTransactions(
        CategoryRule rule,
        IReadOnlyList<CategoryRule> rules,
        IReadOnlyList<Transaction> transactions)
    {
        return transactions
            .Where(transaction => ruleMatcher.FindMatch(transaction, rules)?.Id == rule.Id)
            .OrderByDescending(transaction => transaction.PostedDate)
            .ThenBy(transaction => transaction.OriginalDescription)
            .ToArray();
    }

    private IReadOnlyList<Transaction> FindMatchingTransactions(
        CategoryRule rule,
        IReadOnlyList<Transaction> transactions)
    {
        return transactions
            .Where(transaction => ruleMatcher.IsMatch(transaction, rule))
            .OrderByDescending(transaction => transaction.PostedDate)
            .ThenBy(transaction => transaction.OriginalDescription)
            .ToArray();
    }

    private Dictionary<Guid, IReadOnlyList<Transaction>> BuildWinningTransactionsByRuleId(
        IReadOnlyList<CategoryRule> rules,
        IReadOnlyList<Transaction> transactions)
    {
        return transactions
            .Select(transaction => new
            {
                Transaction = transaction,
                RuleId = ruleMatcher.FindMatch(transaction, rules)?.Id
            })
            .Where(match => match.RuleId is not null)
            .GroupBy(match => match.RuleId!.Value)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<Transaction>)group
                    .Select(match => match.Transaction)
                    .OrderByDescending(transaction => transaction.PostedDate)
                    .ThenBy(transaction => transaction.OriginalDescription)
                    .ToArray());
    }

    private IReadOnlyList<ManagedCategoryRule> BuildRuleRows(
        IReadOnlyList<CategoryRule> rules,
        IReadOnlyList<Transaction> transactions)
    {
        var matchingTransactionsByRuleId = BuildWinningTransactionsByRuleId(rules, transactions);

        return rules
            .Select(rule => new ManagedCategoryRule(
                rule.Id,
                rule.Pattern,
                rule.MatchType,
                rule.CategoryId,
                matchingTransactionsByRuleId.GetValueOrDefault(rule.Id)?.Count ?? 0))
            .ToArray();
    }

    private static IReadOnlyList<ManagedRuleTransactionPreview> BuildPreviewRows(
        IReadOnlyList<Transaction> transactions,
        IReadOnlyList<TransactionCategoryAssignment> assignments,
        int newCategoryId,
        IReadOnlySet<Guid>? winningTransactionIds = null)
    {
        var assignmentsByTransactionId = assignments.ToDictionary(assignment => assignment.TransactionId);

        return transactions
            .Select(transaction => new ManagedRuleTransactionPreview(
                transaction.Id,
                transaction.PostedDate,
                transaction.OriginalDescription,
                transaction.CardAccountId,
                transaction.Amount,
                assignmentsByTransactionId.GetValueOrDefault(transaction.Id)?.CategoryId
                    ?? BuiltInCategoryIds.Other,
                newCategoryId,
                winningTransactionIds is null || winningTransactionIds.Contains(transaction.Id)))
            .ToArray();
    }

    private static TransactionCategoryAssignment CreateAppliedAssignment(
        Transaction transaction,
        TransactionCategoryAssignment? existingAssignment,
        string rulePattern,
        int categoryId)
    {
        var assignment = existingAssignment
            ?? new TransactionCategoryAssignment
            {
                TransactionId = transaction.Id
            };

        assignment.CategoryId = categoryId;
        assignment.Confidence = 1m;
        assignment.NeedsReview = false;
        assignment.Source = CategorizationSource.LocalRules;
        assignment.Explanation = $"Applied category rule '{rulePattern}'.";
        assignment.UpdatedAtUtc = DateTimeOffset.UtcNow;

        return assignment;
    }

    private static CategoryRule? SelectRule(
        IReadOnlyList<CategoryRule> rules,
        Guid? selectedRuleId)
    {
        if (selectedRuleId is Guid ruleId)
        {
            var selectedRule = rules.FirstOrDefault(rule => rule.Id == ruleId);
            if (selectedRule is not null)
            {
                return selectedRule;
            }
        }

        return rules.FirstOrDefault();
    }

    private static IReadOnlyList<CategoryRule> ApplyDraftRule(
        IReadOnlyList<CategoryRule> rules,
        CategoryRule? selectedRule,
        CategoryRuleUpdate? draftUpdate)
    {
        if (selectedRule is null || draftUpdate is null || draftUpdate.RuleId != selectedRule.Id)
        {
            return rules;
        }

        var draftRule = new CategoryRule
        {
            Id = selectedRule.Id,
            Pattern = NormalizePattern(draftUpdate.Pattern),
            MatchType = draftUpdate.MatchType,
            CategoryId = draftUpdate.CategoryId
        };

        return rules
            .Select(rule => rule.Id == draftRule.Id ? draftRule : rule)
            .ToArray();
    }
}
