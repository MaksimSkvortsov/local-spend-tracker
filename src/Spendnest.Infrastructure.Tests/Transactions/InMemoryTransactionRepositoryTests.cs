namespace Spendnest.Infrastructure.Tests.Transactions;

using FluentAssertions;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Transactions;

public class InMemoryTransactionRepositoryTests
{
    [Fact]
    public async Task ListAsync_ShouldFilterByInclusiveDateRange()
    {
        var repository = new InMemoryTransactionRepository();
        await repository.AddRangeAsync(
            [
                TransactionOn(new DateOnly(2026, 6, 30), "Before"),
                TransactionOn(new DateOnly(2026, 7, 1), "Start"),
                TransactionOn(new DateOnly(2026, 7, 31), "End"),
                TransactionOn(new DateOnly(2026, 8, 1), "After")
            ],
            CancellationToken.None);

        var result = await repository.ListAsync(
            new TransactionQuery
            {
                StartDate = new DateOnly(2026, 7, 1),
                EndDate = new DateOnly(2026, 7, 31)
            },
            CancellationToken.None);

        result.Select(transaction => transaction.OriginalDescription)
            .Should().Equal("Start", "End");
    }

    [Fact]
    public async Task ListAsync_ShouldAllowOpenEndedDateRanges()
    {
        var repository = new InMemoryTransactionRepository();
        await repository.AddRangeAsync(
            [
                TransactionOn(new DateOnly(2026, 7, 1), "First"),
                TransactionOn(new DateOnly(2026, 7, 2), "Second")
            ],
            CancellationToken.None);

        var result = await repository.ListAsync(
            new TransactionQuery
            {
                StartDate = new DateOnly(2026, 7, 2)
            },
            CancellationToken.None);

        result.Should().ContainSingle()
            .Which.OriginalDescription.Should().Be("Second");
    }

    private static Transaction TransactionOn(
        DateOnly postedDate,
        string description)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            CardAccountId = Guid.NewGuid(),
            PostedDate = postedDate,
            OriginalDescription = description,
            Amount = 10m,
            ImportedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
