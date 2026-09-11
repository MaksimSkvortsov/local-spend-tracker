namespace Spendnest.Application.Tests.Rules;

using FluentAssertions;
using Spendnest.Application.Categorization;
using Spendnest.Application.Rules;
using Spendnest.Application.Tests.TestDoubles;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Categorization;

public class CategoryRuleManagementServiceTests
{
    [Fact]
    public async Task LoadAsync_ShouldPreviewTransactionsWhereSelectedRuleWins()
    {
        var ruleRepository = new FakeCategoryRuleRepository();
        var selectedRule = Rule("DOORDASH", BuiltInCategoryIds.Other, CategoryRuleMatchType.Prefix);
        var shadowingRule = Rule("DOORDASH CAVIAR", BuiltInCategoryIds.Entertainment, CategoryRuleMatchType.Prefix);
        await ruleRepository.AddAsync(selectedRule, CancellationToken.None);
        await ruleRepository.AddAsync(shadowingRule, CancellationToken.None);
        var transactionRepository = new FakeTransactionRepository();
        var winningTransaction = Transaction("DOORDASH CHIPOTLE", 12m);
        var shadowedTransaction = Transaction("DOORDASH CAVIAR", 24m);
        await transactionRepository.AddRangeAsync(
            [winningTransaction, shadowedTransaction],
            CancellationToken.None);
        var assignmentRepository = new FakeTransactionCategoryAssignmentRepository();
        await assignmentRepository.SaveAsync(
            Assignment(winningTransaction.Id, BuiltInCategoryIds.Groceries),
            CancellationToken.None);
        var service = CreateService(ruleRepository, transactionRepository, assignmentRepository);

        var result = await service.LoadAsync(selectedRule.Id, CancellationToken.None);

        result.SelectedRuleId.Should().Be(selectedRule.Id);
        result.Rules.Should().ContainSingle(rule =>
            rule.Id == selectedRule.Id
            && rule.MatchCount == 1);
        result.Rules.Should().ContainSingle(rule =>
            rule.Id == shadowingRule.Id
            && rule.MatchCount == 1);
        result.PreviewRows.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            TransactionId = winningTransaction.Id,
            CurrentCategoryId = BuiltInCategoryIds.Groceries,
            NewCategoryId = BuiltInCategoryIds.Other
        });
    }

    [Fact]
    public async Task SaveAndApplyAsync_ShouldUpdateRuleAndAssignmentsForWinningMatches()
    {
        var ruleRepository = new FakeCategoryRuleRepository();
        var selectedRule = Rule("DOORDASH", BuiltInCategoryIds.Other, CategoryRuleMatchType.Prefix);
        var shadowingRule = Rule("DOORDASH CAVIAR", BuiltInCategoryIds.Entertainment, CategoryRuleMatchType.Prefix);
        await ruleRepository.AddAsync(selectedRule, CancellationToken.None);
        await ruleRepository.AddAsync(shadowingRule, CancellationToken.None);
        var transactionRepository = new FakeTransactionRepository();
        var winningTransaction = Transaction("DOORDASH CHIPOTLE", 12m);
        var shadowedTransaction = Transaction("DOORDASH CAVIAR", 24m);
        await transactionRepository.AddRangeAsync(
            [winningTransaction, shadowedTransaction],
            CancellationToken.None);
        var assignmentRepository = new FakeTransactionCategoryAssignmentRepository();
        await assignmentRepository.SaveAsync(
            Assignment(winningTransaction.Id, BuiltInCategoryIds.Groceries),
            CancellationToken.None);
        var service = CreateService(ruleRepository, transactionRepository, assignmentRepository);

        var count = await service.SaveAndApplyAsync(
            new CategoryRuleUpdate(
                selectedRule.Id,
                "DOORDASH",
                CategoryRuleMatchType.Contains,
                BuiltInCategoryIds.RestaurantsAndCoffee),
            CancellationToken.None);

        count.Should().Be(1);
        var updatedRule = (await ruleRepository.ListAsync(CancellationToken.None))
            .Single(rule => rule.Id == selectedRule.Id);
        updatedRule.Pattern.Should().Be("DOORDASH");
        updatedRule.MatchType.Should().Be(CategoryRuleMatchType.Contains);
        updatedRule.CategoryId.Should().Be(BuiltInCategoryIds.RestaurantsAndCoffee);
        ruleRepository.AppliedAssignments.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            TransactionId = winningTransaction.Id,
            CategoryId = BuiltInCategoryIds.RestaurantsAndCoffee,
            Confidence = 1m,
            NeedsReview = false,
            Source = CategorizationSource.LocalRules,
            Explanation = "Applied category rule 'DOORDASH'."
        });
    }

    [Fact]
    public async Task SaveAndApplyAsync_ShouldSaveRuleWhenThereAreNoMatches()
    {
        var ruleRepository = new FakeCategoryRuleRepository();
        var selectedRule = Rule("DOORDASH", BuiltInCategoryIds.Other, CategoryRuleMatchType.Prefix);
        await ruleRepository.AddAsync(selectedRule, CancellationToken.None);
        var service = CreateService(ruleRepository);

        var count = await service.SaveAndApplyAsync(
            new CategoryRuleUpdate(
                selectedRule.Id,
                "DD * DOORDASH",
                CategoryRuleMatchType.Contains,
                BuiltInCategoryIds.RestaurantsAndCoffee),
            CancellationToken.None);

        count.Should().Be(0);
        var updatedRule = (await ruleRepository.ListAsync(CancellationToken.None))
            .Should()
            .ContainSingle()
            .Which;
        updatedRule.Pattern.Should().Be("DD * DOORDASH");
        updatedRule.MatchType.Should().Be(CategoryRuleMatchType.Contains);
        updatedRule.CategoryId.Should().Be(BuiltInCategoryIds.RestaurantsAndCoffee);
    }

    [Fact]
    public async Task SaveAndApplyAsync_ShouldRejectUnknownCategory()
    {
        var ruleRepository = new FakeCategoryRuleRepository();
        var selectedRule = Rule("DOORDASH", BuiltInCategoryIds.Other, CategoryRuleMatchType.Prefix);
        await ruleRepository.AddAsync(selectedRule, CancellationToken.None);
        var service = CreateService(ruleRepository);

        var act = () => service.SaveAndApplyAsync(
            new CategoryRuleUpdate(
                selectedRule.Id,
                "DOORDASH",
                CategoryRuleMatchType.Prefix,
                9999),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Selected category was not found.");
    }

    private static CategoryRuleManagementService CreateService(
        FakeCategoryRuleRepository? ruleRepository = null,
        FakeTransactionRepository? transactionRepository = null,
        FakeTransactionCategoryAssignmentRepository? assignmentRepository = null)
    {
        var merchantCodeResolver = new TransactionMerchantCodeResolver();
        ruleRepository ??= new FakeCategoryRuleRepository();

        return new CategoryRuleManagementService(
            ruleRepository,
            new FakeCategoryRuleApplicationStore(ruleRepository),
            new FakeCategoryRepository(),
            transactionRepository ?? new FakeTransactionRepository(),
            assignmentRepository ?? new FakeTransactionCategoryAssignmentRepository(),
            new FakeCardAccountRepository(),
            new LocalCategoryRuleMatcher(merchantCodeResolver));
    }

    private static CategoryRule Rule(
        string pattern,
        int categoryId,
        CategoryRuleMatchType matchType)
    {
        return new CategoryRule
        {
            Pattern = pattern,
            CategoryId = categoryId,
            MatchType = matchType
        };
    }

    private static Transaction Transaction(
        string description,
        decimal amount)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            CardAccountId = Guid.NewGuid(),
            PostedDate = new DateOnly(2026, 7, 24),
            OriginalDescription = description,
            Amount = amount,
            ImportedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static TransactionCategoryAssignment Assignment(
        Guid transactionId,
        int categoryId)
    {
        return new TransactionCategoryAssignment
        {
            TransactionId = transactionId,
            CategoryId = categoryId,
            Confidence = 1m,
            NeedsReview = false,
            Source = CategorizationSource.LocalRules,
            Explanation = "Existing category."
        };
    }
}
