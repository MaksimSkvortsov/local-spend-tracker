namespace Spendnest.Infrastructure.Tests.Categorization;

using FluentAssertions;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Categorization;

public class TransactionCategorizationServiceTests
{
    [Fact]
    public async Task CategorizeAsync_ShouldUseLocalFirstAndSendOnlyUnresolvedTransactionsToAi()
    {
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
        var service = CreateService(aiCategorizer);

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
        ITransactionCategoryAssignmentRepository assignmentRepository)
    {
        return new TransactionCategorizationService(
            new LocalTransactionCategorizer(
                new InMemoryCategoryRuleRepository(),
                new KeywordTransactionCategoryMapper()),
            aiCategorizer,
            assignmentRepository);
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
}
