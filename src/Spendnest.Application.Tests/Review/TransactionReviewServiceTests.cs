namespace Spendnest.Application.Tests.Review;

using FluentAssertions;
using Spendnest.Application.Review;
using Spendnest.Application.Tests.TestDoubles;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Categorization;

public class TransactionReviewServiceTests
{
    [Fact]
    public async Task ListNeedsReviewAsync_ShouldReturnTransactionsMarkedForReview()
    {
        var repository = new FakeTransactionRepository();
        var transaction = Transaction("MYSTERY PLACE");
        await repository.AddRangeAsync([transaction], CancellationToken.None);
        var assignmentRepository = new FakeTransactionCategoryAssignmentRepository();
        await assignmentRepository.SaveAsync(
            new TransactionCategoryAssignment
            {
                TransactionId = transaction.Id,
                CategoryId = BuiltInCategoryIds.Other,
                Source = CategorizationSource.Unresolved,
                Confidence = 0m,
                NeedsReview = true,
                Explanation = "AI categorization failed."
            },
            CancellationToken.None);
        var service = new TransactionReviewService(
            repository,
            assignmentRepository,
            new FakeCategoryRuleRepository(),
            new TransactionMerchantCodeResolver());

        var result = await service.ListNeedsReviewAsync(CancellationToken.None);

        result.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            TransactionId = transaction.Id,
            Description = "MYSTERY PLACE",
            CategoryId = BuiltInCategoryIds.Other,
            Source = CategorizationSource.Unresolved,
            Confidence = 0m,
            Explanation = "AI categorization failed."
        });
    }

    [Fact]
    public async Task CountNeedsReviewAsync_ShouldReturnReviewCountForExistingTransactions()
    {
        var repository = new FakeTransactionRepository();
        var reviewTransaction = Transaction("MYSTERY PLACE");
        var resolvedTransaction = Transaction("KNOWN PLACE");
        await repository.AddRangeAsync([reviewTransaction, resolvedTransaction], CancellationToken.None);
        var assignmentRepository = new FakeTransactionCategoryAssignmentRepository();
        await assignmentRepository.SaveAsync(
            new TransactionCategoryAssignment
            {
                TransactionId = reviewTransaction.Id,
                CategoryId = BuiltInCategoryIds.Other,
                Source = CategorizationSource.Unresolved,
                Confidence = 0m,
                NeedsReview = true,
                Explanation = "AI categorization failed."
            },
            CancellationToken.None);
        await assignmentRepository.SaveAsync(
            new TransactionCategoryAssignment
            {
                TransactionId = resolvedTransaction.Id,
                CategoryId = BuiltInCategoryIds.Groceries,
                Source = CategorizationSource.LocalRules,
                Confidence = 1m,
                NeedsReview = false,
                Explanation = "Known merchant."
            },
            CancellationToken.None);
        await assignmentRepository.SaveAsync(
            new TransactionCategoryAssignment
            {
                TransactionId = Guid.NewGuid(),
                CategoryId = BuiltInCategoryIds.Other,
                Source = CategorizationSource.Unresolved,
                Confidence = 0m,
                NeedsReview = true,
                Explanation = "Orphaned assignment."
            },
            CancellationToken.None);
        var service = new TransactionReviewService(
            repository,
            assignmentRepository,
            new FakeCategoryRuleRepository(),
            new TransactionMerchantCodeResolver());

        var count = await service.CountNeedsReviewAsync(CancellationToken.None);

        count.Should().Be(1);
    }

    [Fact]
    public async Task SetCategoryAsync_ShouldSaveAssignmentAndRememberExactRule()
    {
        var repository = new FakeTransactionRepository();
        var assignmentRepository = new FakeTransactionCategoryAssignmentRepository();
        var ruleRepository = new FakeCategoryRuleRepository();
        var transaction = Transaction("MYSTERY PLACE");
        await repository.AddRangeAsync([transaction], CancellationToken.None);
        await assignmentRepository.SaveAsync(
            new TransactionCategoryAssignment
            {
                TransactionId = transaction.Id,
                CategoryId = BuiltInCategoryIds.Other,
                Source = CategorizationSource.Unresolved,
                Confidence = 0m,
                NeedsReview = true,
                Explanation = "Needs review."
            },
            CancellationToken.None);
        var service = new TransactionReviewService(
            repository,
            assignmentRepository,
            ruleRepository,
            new TransactionMerchantCodeResolver());

        await service.SetCategoryAsync(
            transaction.Id,
            BuiltInCategoryIds.Entertainment,
            rememberRule: true,
            CancellationToken.None);

        var assignment = await assignmentRepository.GetByTransactionIdAsync(transaction.Id, CancellationToken.None);
        var rules = await ruleRepository.ListAsync(CancellationToken.None);

        assignment.Should().NotBeNull();
        assignment!.CategoryId.Should().Be(BuiltInCategoryIds.Entertainment);
        assignment.NeedsReview.Should().BeFalse();
        assignment.Source.Should().Be(CategorizationSource.LocalRules);
        assignment.Confidence.Should().Be(1m);
        rules.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Pattern = "MYSTERY PLACE",
            CategoryId = BuiltInCategoryIds.Entertainment,
            MatchType = CategoryRuleMatchType.Exact
        });
    }

    [Fact]
    public async Task ConfirmAsync_ShouldClearReviewAndOptionallyRememberCurrentCategory()
    {
        var repository = new FakeTransactionRepository();
        var assignmentRepository = new FakeTransactionCategoryAssignmentRepository();
        var ruleRepository = new FakeCategoryRuleRepository();
        var transaction = Transaction("MYSTERY PLACE");
        await repository.AddRangeAsync([transaction], CancellationToken.None);
        await assignmentRepository.SaveAsync(
            new TransactionCategoryAssignment
            {
                TransactionId = transaction.Id,
                CategoryId = BuiltInCategoryIds.Other,
                Source = CategorizationSource.Unresolved,
                Confidence = 0m,
                NeedsReview = true,
                Explanation = "Needs review."
            },
            CancellationToken.None);
        var service = new TransactionReviewService(
            repository,
            assignmentRepository,
            ruleRepository,
            new TransactionMerchantCodeResolver());

        await service.ConfirmAsync(
            transaction.Id,
            rememberRule: true,
            CancellationToken.None);

        var assignment = await assignmentRepository.GetByTransactionIdAsync(transaction.Id, CancellationToken.None);
        var rules = await ruleRepository.ListAsync(CancellationToken.None);

        assignment.Should().NotBeNull();
        assignment!.NeedsReview.Should().BeFalse();
        assignment.Source.Should().Be(CategorizationSource.LocalRules);
        rules.Should().ContainSingle(rule =>
            rule.Pattern == "MYSTERY PLACE"
            && rule.CategoryId == BuiltInCategoryIds.Other
            && rule.MatchType == CategoryRuleMatchType.Exact);
    }

    [Fact]
    public async Task ConfirmAsync_ShouldNotRememberRuleWhenDisabled()
    {
        var repository = new FakeTransactionRepository();
        var assignmentRepository = new FakeTransactionCategoryAssignmentRepository();
        var ruleRepository = new FakeCategoryRuleRepository();
        var transaction = Transaction("MYSTERY PLACE");
        await repository.AddRangeAsync([transaction], CancellationToken.None);
        await assignmentRepository.SaveAsync(
            new TransactionCategoryAssignment
            {
                TransactionId = transaction.Id,
                CategoryId = BuiltInCategoryIds.Entertainment,
                Source = CategorizationSource.Ai,
                Confidence = 0.6m,
                NeedsReview = true,
                Explanation = "Needs review."
            },
            CancellationToken.None);
        var service = new TransactionReviewService(
            repository,
            assignmentRepository,
            ruleRepository,
            new TransactionMerchantCodeResolver());

        await service.ConfirmAsync(
            transaction.Id,
            rememberRule: false,
            CancellationToken.None);

        var assignment = await assignmentRepository.GetByTransactionIdAsync(transaction.Id, CancellationToken.None);
        var rules = await ruleRepository.ListAsync(CancellationToken.None);

        assignment.Should().NotBeNull();
        assignment!.CategoryId.Should().Be(BuiltInCategoryIds.Entertainment);
        assignment.NeedsReview.Should().BeFalse();
        assignment.Source.Should().Be(CategorizationSource.LocalRules);
        assignment.Confidence.Should().Be(1m);
        assignment.Explanation.Should().Be("Confirmed during review.");
        rules.Should().BeEmpty();
    }

    [Fact]
    public async Task ConfirmAsync_ShouldRejectTransactionsWithoutAssignments()
    {
        var repository = new FakeTransactionRepository();
        var transaction = Transaction("MYSTERY PLACE");
        await repository.AddRangeAsync([transaction], CancellationToken.None);
        var service = new TransactionReviewService(
            repository,
            new FakeTransactionCategoryAssignmentRepository(),
            new FakeCategoryRuleRepository(),
            new TransactionMerchantCodeResolver());

        var act = () => service.ConfirmAsync(
            transaction.Id,
            rememberRule: false,
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Transaction '{transaction.Id}' does not have a category assignment.");
    }

    [Fact]
    public async Task ConfirmAsync_ShouldRejectAssignmentsWithoutCategory()
    {
        var repository = new FakeTransactionRepository();
        var assignmentRepository = new FakeTransactionCategoryAssignmentRepository();
        var transaction = Transaction("MYSTERY PLACE");
        await repository.AddRangeAsync([transaction], CancellationToken.None);
        await assignmentRepository.SaveAsync(
            new TransactionCategoryAssignment
            {
                TransactionId = transaction.Id,
                CategoryId = 0,
                Source = CategorizationSource.Unresolved,
                Confidence = 0m,
                NeedsReview = true,
                Explanation = "Needs category."
            },
            CancellationToken.None);
        var service = new TransactionReviewService(
            repository,
            assignmentRepository,
            new FakeCategoryRuleRepository(),
            new TransactionMerchantCodeResolver());

        var act = () => service.ConfirmAsync(
            transaction.Id,
            rememberRule: false,
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Transaction does not have a category to confirm.");
    }

    [Fact]
    public async Task SetCategoryAsync_ShouldNotRememberRuleWhenDisabled()
    {
        var repository = new FakeTransactionRepository();
        var assignmentRepository = new FakeTransactionCategoryAssignmentRepository();
        var ruleRepository = new FakeCategoryRuleRepository();
        var transaction = Transaction("MYSTERY PLACE");
        await repository.AddRangeAsync([transaction], CancellationToken.None);
        var service = new TransactionReviewService(
            repository,
            assignmentRepository,
            ruleRepository,
            new TransactionMerchantCodeResolver());

        await service.SetCategoryAsync(
            transaction.Id,
            BuiltInCategoryIds.Entertainment,
            rememberRule: false,
            CancellationToken.None);

        var assignment = await assignmentRepository.GetByTransactionIdAsync(transaction.Id, CancellationToken.None);
        var rules = await ruleRepository.ListAsync(CancellationToken.None);

        assignment.Should().NotBeNull();
        assignment!.CategoryId.Should().Be(BuiltInCategoryIds.Entertainment);
        assignment.NeedsReview.Should().BeFalse();
        assignment.Source.Should().Be(CategorizationSource.LocalRules);
        assignment.Confidence.Should().Be(1m);
        assignment.Explanation.Should().Be("Set during review.");
        rules.Should().BeEmpty();
    }

    [Fact]
    public async Task SetCategoryAsync_ShouldRejectUnknownCategoryIds()
    {
        var repository = new FakeTransactionRepository();
        var transaction = Transaction("MYSTERY PLACE");
        await repository.AddRangeAsync([transaction], CancellationToken.None);
        var service = new TransactionReviewService(
            repository,
            new FakeTransactionCategoryAssignmentRepository(),
            new FakeCategoryRuleRepository(),
            new TransactionMerchantCodeResolver());

        var act = () => service.SetCategoryAsync(
            transaction.Id,
            9999,
            rememberRule: false,
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Unknown category id*");
    }

    private static Transaction Transaction(string description)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            CardAccountId = Guid.NewGuid(),
            PostedDate = new DateOnly(2026, 7, 24),
            OriginalDescription = description,
            Amount = 10m,
            ImportedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
