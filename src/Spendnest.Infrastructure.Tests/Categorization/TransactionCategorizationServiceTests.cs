namespace Spendnest.Infrastructure.Tests.Categorization;

using FluentAssertions;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Progress;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Categorization;

public class TransactionCategorizationServiceTests
{
    [Fact]
    public async Task CategorizeAsync_ShouldUseLocalFirstAndSendOnlyUnresolvedTransactionsToAi()
    {
        var ruleRepository = new InMemoryCategoryRuleRepository();
        await ruleRepository.AddAsync(new CategoryRule
        {
            Pattern = "BULK MART",
            CategoryId = BuiltInCategoryIds.Groceries,
            MatchType = CategoryRuleMatchType.Exact
        }, CancellationToken.None);
        var localTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OriginalDescription = "BULK MART #0218 RIVERTON VA",
            Amount = 141.83m
        };
        var unresolvedTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OriginalDescription = "MYSTERY PLACE",
            Amount = 19.99m
        };
        var aiCategorizer = new RecordingTransactionCategorizer(transactions =>
            transactions.Select(transaction => new TransactionCategorization(
                transaction.Id,
                BuiltInCategoryIds.Entertainment,
                0.82m,
                false,
                CategorizationSource.LocalAi,
                "Resolved by test AI.")).ToArray());
        var service = CreateService(aiCategorizer, categoryRuleRepository: ruleRepository);

        var result = await service.CategorizeAsync([localTransaction, unresolvedTransaction], CancellationToken.None);

        aiCategorizer.SeenTransactionIds.Should().Equal(unresolvedTransaction.Id);
        result.Should().Contain(categorization =>
            categorization.TransactionId == localTransaction.Id
            && categorization.CategoryId == BuiltInCategoryIds.Groceries
            && categorization.Source == CategorizationSource.LocalRules);
        result.Should().Contain(categorization =>
            categorization.TransactionId == unresolvedTransaction.Id
            && categorization.CategoryId == BuiltInCategoryIds.Entertainment
            && categorization.Source == CategorizationSource.LocalAi);
    }

    [Fact]
    public async Task CategorizeAsync_ShouldReportAiProgressForUnresolvedTransactions()
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OriginalDescription = "MYSTERY PLACE",
            Amount = 19.99m
        };
        var aiCategorizer = new RecordingTransactionCategorizer(transactions =>
            transactions.Select(item => new TransactionCategorization(
                item.Id,
                BuiltInCategoryIds.Other,
                0.82m,
                false,
                CategorizationSource.Ai,
                "Resolved by test AI.")).ToArray());
        var service = CreateService(aiCategorizer);
        var progress = new RecordingProgress();

        await service.CategorizeAsync([transaction], progress, CancellationToken.None);

        progress.Events.Should().ContainSingle().Which.Should().BeEquivalentTo(new FileUploadProgress(
            FileUploadProgressStage.CategorizingWithAi,
            "Categorizing with AI",
            0,
            1));
    }

    [Fact]
    public async Task CategorizeAsync_ShouldRememberHighConfidenceAiResultsAsMerchantRules()
    {
        var ruleRepository = new InMemoryCategoryRuleRepository();
        var firstTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OriginalDescription = "TINY CINEMA #7781",
            Amount = 19.99m
        };
        var secondTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OriginalDescription = "TINY CINEMA #9912",
            Amount = 21.99m
        };
        var aiCategorizer = new RecordingTransactionCategorizer(transactions =>
            transactions.Select(transaction => new TransactionCategorization(
                transaction.Id,
                BuiltInCategoryIds.Entertainment,
                0.91m,
                false,
                CategorizationSource.Ai,
                "Resolved by test AI.")).ToArray());
        var service = CreateService(aiCategorizer, categoryRuleRepository: ruleRepository);

        var firstResult = await service.CategorizeAsync([firstTransaction], CancellationToken.None);
        var secondResult = await service.CategorizeAsync([secondTransaction], CancellationToken.None);

        firstResult.Should().ContainSingle(categorization =>
            categorization.TransactionId == firstTransaction.Id
            && categorization.Source == CategorizationSource.Ai);
        secondResult.Should().ContainSingle(categorization =>
            categorization.TransactionId == secondTransaction.Id
            && categorization.CategoryId == BuiltInCategoryIds.Entertainment
            && categorization.Source == CategorizationSource.LocalRules);
        aiCategorizer.SeenTransactionIds.Should().Equal(firstTransaction.Id);
        var rules = await ruleRepository.ListAsync(CancellationToken.None);
        rules.Should().ContainSingle(rule =>
            rule.Pattern == "TINY CINEMA"
            && rule.CategoryId == BuiltInCategoryIds.Entertainment
            && rule.MatchType == CategoryRuleMatchType.Exact);
    }

    [Fact]
    public async Task CategorizeAsync_ShouldMarkUnresolvedWhenAiFails()
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OriginalDescription = "MYSTERY PLACE",
            Amount = 19.99m
        };
        var service = CreateService(new FailingTransactionCategorizer());

        var result = await service.CategorizeAsync([transaction], CancellationToken.None);

        result.Should().ContainSingle().Which.Should().BeEquivalentTo(new TransactionCategorization(
            transaction.Id,
            BuiltInCategoryIds.Other,
            0m,
            true,
            CategorizationSource.Unresolved,
            "AI categorization failed."));
    }

    [Fact]
    public async Task CategorizeAsync_ShouldMarkUnresolvedWhenAiTimesOut()
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OriginalDescription = "MYSTERY PLACE",
            Amount = 19.99m
        };
        var service = CreateService(new TimeoutTransactionCategorizer());

        var result = await service.CategorizeAsync([transaction], CancellationToken.None);

        result.Should().ContainSingle().Which.Should().BeEquivalentTo(new TransactionCategorization(
            transaction.Id,
            BuiltInCategoryIds.Other,
            0m,
            true,
            CategorizationSource.Unresolved,
            "AI categorization timed out."));
    }

    [Fact]
    public async Task CategorizeAsync_ShouldPropagateCallerCancellation()
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OriginalDescription = "MYSTERY PLACE",
            Amount = 19.99m
        };
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var service = CreateService(new CanceledTransactionCategorizer());

        var act = () => service.CategorizeAsync([transaction], cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task CategorizeAsync_ShouldRejectInvalidAiCategoryIdsForReview()
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OriginalDescription = "MYSTERY PLACE",
            Amount = 19.99m
        };
        var service = CreateService(new RecordingTransactionCategorizer(transactions =>
            transactions.Select(item => new TransactionCategorization(
                item.Id,
                9999,
                0.99m,
                false,
                CategorizationSource.LocalAi,
                "Invalid test category.")).ToArray()));

        var result = await service.CategorizeAsync([transaction], CancellationToken.None);

        result.Should().ContainSingle().Which.Should().BeEquivalentTo(new TransactionCategorization(
            transaction.Id,
            BuiltInCategoryIds.Other,
            0m,
            true,
            CategorizationSource.Unresolved,
            "Rejected unsupported category id '9999'."));
    }

    [Fact]
    public async Task CategorizeAsync_ShouldPreserveExistingCategoryAssignments()
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OriginalDescription = "MYSTERY PLACE",
            Amount = 19.99m
        };
        var assignmentRepository = new InMemoryTransactionCategoryAssignmentRepository();
        await assignmentRepository.SaveAsync(
            new TransactionCategoryAssignment
            {
                TransactionId = transaction.Id,
                CategoryId = BuiltInCategoryIds.Entertainment,
                Source = CategorizationSource.LocalRules,
                Confidence = 1m,
                NeedsReview = false,
                Explanation = "Set during review."
            },
            CancellationToken.None);
        var aiCategorizer = new RecordingTransactionCategorizer(_ =>
            throw new InvalidOperationException("AI should not be called."));
        var service = CreateService(aiCategorizer, assignmentRepository);

        var result = await service.CategorizeAsync([transaction], CancellationToken.None);

        aiCategorizer.SeenTransactionIds.Should().BeEmpty();
        result.Should().ContainSingle().Which.Should().BeEquivalentTo(new TransactionCategorization(
            transaction.Id,
            BuiltInCategoryIds.Entertainment,
            1m,
            false,
            CategorizationSource.LocalRules,
            "Set during review."));
    }

    private static TransactionCategorizationService CreateService(ITransactionCategorizer aiCategorizer)
    {
        return CreateService(aiCategorizer, new InMemoryTransactionCategoryAssignmentRepository());
    }

    private static TransactionCategorizationService CreateService(
        ITransactionCategorizer aiCategorizer,
        ITransactionCategoryAssignmentRepository? assignmentRepository = null,
        ICategoryRuleRepository? categoryRuleRepository = null)
    {
        var rules = categoryRuleRepository ?? new InMemoryCategoryRuleRepository();
        var merchantCodeResolver = new TransactionMerchantCodeResolver();

        return new TransactionCategorizationService(
            new LocalTransactionCategorizer(
                rules,
                merchantCodeResolver),
            aiCategorizer,
            assignmentRepository ?? new InMemoryTransactionCategoryAssignmentRepository(),
            rules,
            merchantCodeResolver);
    }

    private sealed class RecordingTransactionCategorizer : ITransactionCategorizer
    {
        private readonly Func<IReadOnlyList<Transaction>, IReadOnlyList<TransactionCategorization>> categorize;

        public RecordingTransactionCategorizer(
            Func<IReadOnlyList<Transaction>, IReadOnlyList<TransactionCategorization>> categorize)
        {
            this.categorize = categorize;
        }

        public IReadOnlyList<Guid> SeenTransactionIds { get; private set; } = [];

        public Task<IReadOnlyList<TransactionCategorization>> CategorizeAsync(
            IReadOnlyList<Transaction> transactions,
            CancellationToken cancellationToken)
        {
            SeenTransactionIds = transactions.Select(transaction => transaction.Id).ToArray();

            return Task.FromResult(categorize(transactions));
        }
    }

    private sealed class FailingTransactionCategorizer : ITransactionCategorizer
    {
        public Task<IReadOnlyList<TransactionCategorization>> CategorizeAsync(
            IReadOnlyList<Transaction> transactions,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("AI unavailable.");
        }
    }

    private sealed class TimeoutTransactionCategorizer : ITransactionCategorizer
    {
        public Task<IReadOnlyList<TransactionCategorization>> CategorizeAsync(
            IReadOnlyList<Transaction> transactions,
            CancellationToken cancellationToken)
        {
            throw new TimeoutException("AI timeout.");
        }
    }

    private sealed class CanceledTransactionCategorizer : ITransactionCategorizer
    {
        public Task<IReadOnlyList<TransactionCategorization>> CategorizeAsync(
            IReadOnlyList<Transaction> transactions,
            CancellationToken cancellationToken)
        {
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private sealed class RecordingProgress : IProgress<FileUploadProgress>
    {
        private readonly List<FileUploadProgress> events = [];

        public IReadOnlyList<FileUploadProgress> Events => events;

        public void Report(FileUploadProgress value)
        {
            events.Add(value);
        }
    }
}
