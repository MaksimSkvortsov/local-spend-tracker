namespace Spendnest.Application.Tests.Categorization;

using FluentAssertions;
using Spendnest.Application.Categorization;
using Spendnest.Application.Tests.TestDoubles;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;

public class TransactionCategorizationApplierTests
{
    [Fact]
    public async Task ApplyAsync_ShouldSaveCategoryAssignment()
    {
        var transaction = Transaction("MYSTERY PLACE");
        var repository = new FakeTransactionCategoryAssignmentRepository();
        var applier = new TransactionCategorizationApplier(repository);

        await applier.ApplyAsync(
            [
                new TransactionCategorization(
                    transaction.Id,
                    BuiltInCategoryIds.Other,
                    0.42m,
                    true,
                    CategorizationSource.Unresolved,
                    "Needs human review.")
            ],
            CancellationToken.None);

        var assignment = await repository.GetByTransactionIdAsync(transaction.Id, CancellationToken.None);

        assignment.Should().NotBeNull();
        assignment!.CategoryId.Should().Be(BuiltInCategoryIds.Other);
        assignment.Confidence.Should().Be(0.42m);
        assignment.Source.Should().Be(CategorizationSource.Unresolved);
        assignment.NeedsReview.Should().BeTrue();
        assignment.Explanation.Should().Be("Needs human review.");
    }

    [Fact]
    public async Task ApplyAsync_ShouldRejectNullCategorizations()
    {
        var applier = new TransactionCategorizationApplier(new FakeTransactionCategoryAssignmentRepository());

        var act = () => applier.ApplyAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("categorizations");
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
