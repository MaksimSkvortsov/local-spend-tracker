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
                BuiltInCategoryCodes.Entertainment,
                0.82m,
                false,
                CategorizationSource.FakeAi,
                "Resolved by test AI.")).ToArray());
        var service = CreateService(aiCategorizer);

        var result = await service.CategorizeAsync([localTransaction, unresolvedTransaction], CancellationToken.None);

        aiCategorizer.SeenTransactionIds.Should().Equal(unresolvedTransaction.Id);
        result.Should().Contain(decision =>
            decision.TransactionId == localTransaction.Id
            && decision.CategoryCode == BuiltInCategoryCodes.Groceries
            && decision.Source == CategorizationSource.LocalRules);
        result.Should().Contain(decision =>
            decision.TransactionId == unresolvedTransaction.Id
            && decision.CategoryCode == BuiltInCategoryCodes.Entertainment
            && decision.Source == CategorizationSource.FakeAi);
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
            BuiltInCategoryCodes.Other,
            0m,
            true,
            CategorizationSource.Unresolved,
            "AI categorization failed."));
    }

    [Fact]
    public async Task CategorizeAsync_ShouldRejectInvalidAiCategoryCodesForReview()
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
                "NotReal",
                0.99m,
                false,
                CategorizationSource.FakeAi,
                "Invalid test category.")).ToArray()));

        var result = await service.CategorizeAsync([transaction], CancellationToken.None);

        result.Should().ContainSingle().Which.Should().BeEquivalentTo(new TransactionCategorization(
            transaction.Id,
            BuiltInCategoryCodes.Other,
            0m,
            true,
            CategorizationSource.Unresolved,
            "Rejected unsupported category code 'NotReal'."));
    }

    private static TransactionCategorizationService CreateService(ITransactionCategorizer aiCategorizer)
    {
        return new TransactionCategorizationService(
            new LocalTransactionCategorizer(
                new InMemoryCategoryRuleRepository(),
                new KeywordTransactionCategoryMapper()),
            aiCategorizer);
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
