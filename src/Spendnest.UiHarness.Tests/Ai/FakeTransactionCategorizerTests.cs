namespace Spendnest.UiHarness.Tests.Ai;

using FluentAssertions;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Credentials;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Credentials;
using Spendnest.UiHarness.Ai;

public class FakeTransactionCategorizerTests
{
    [Fact]
    public async Task CategorizeAsync_ShouldMatchCatalogMerchantsAndReviewUnknownOnes()
    {
        var categorizer = new FakeTransactionCategorizer(CreateCredentialStore("test-key"));
        var known = CreateTransaction("STARBUCKS STORE 99999");
        var unknown = CreateTransaction("MYSTERY SHOP 42");

        var results = await categorizer.CategorizeAsync([known, unknown], CancellationToken.None);

        results.Should().BeEquivalentTo(
            [
                new
                {
                    TransactionId = known.Id,
                    CategoryId = BuiltInCategoryIds.RestaurantsAndCoffee,
                    NeedsReview = false,
                    Source = CategorizationSource.Ai
                },
                new
                {
                    TransactionId = unknown.Id,
                    CategoryId = BuiltInCategoryIds.Other,
                    NeedsReview = true,
                    Source = CategorizationSource.Ai
                }
            ],
            options => options.ExcludingMissingMembers());
    }

    [Fact]
    public async Task CategorizeAsync_WithoutApiKey_ShouldReportAiUnavailable()
    {
        var categorizer = new FakeTransactionCategorizer(CreateCredentialStore(null));

        var act = () => categorizer.CategorizeAsync([CreateTransaction("STARBUCKS")], CancellationToken.None);

        await act.Should().ThrowAsync<AiCategorizationUnavailableException>();
    }

    private static InMemoryCredentialStore CreateCredentialStore(string? apiKey)
    {
        return new InMemoryCredentialStore(new Dictionary<string, string?>
        {
            [CredentialKeys.OpenAiApiKey] = apiKey
        });
    }

    private static Transaction CreateTransaction(string description)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            PostedDate = new DateOnly(2026, 1, 5),
            OriginalDescription = description,
            Amount = 10m
        };
    }
}
