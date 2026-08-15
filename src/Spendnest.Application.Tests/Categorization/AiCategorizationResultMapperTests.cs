namespace Spendnest.Application.Tests.Categorization;

using FluentAssertions;
using Spendnest.Application.Categorization;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Categorization;

public class AiCategorizationResultMapperTests
{
    [Fact]
    public void MapToUnresolvedTransactions_ShouldMapRepresentativeResultToMatchingMerchantTransactions()
    {
        var mapper = CreateMapper();
        var representative = Transaction("MARKET PLACE");
        var matching = Transaction("MARKET PLACE");

        var results = mapper.MapToUnresolvedTransactions(
            [
                new TransactionCategorization(
                    representative.Id,
                    BuiltInCategoryIds.Groceries,
                    0.88m,
                    false,
                    CategorizationSource.Ai,
                    "Resolved by representative merchant.")
            ],
            [representative],
            [representative, matching]);

        results.Should().HaveCount(2);
        results.Should().OnlyContain(result =>
            result.CategoryId == BuiltInCategoryIds.Groceries
            && result.Source == CategorizationSource.Ai
            && result.Explanation == "Resolved by representative merchant.");
        results.Select(result => result.TransactionId).Should().BeEquivalentTo(
            [
                representative.Id,
                matching.Id
            ]);
    }

    [Fact]
    public void MapToUnresolvedTransactions_ShouldPreferLongestLearnedPrefixMatch()
    {
        var mapper = CreateMapper();
        var shortPrefixRepresentative = Transaction("TINY CINEMA #100");
        var longPrefixRepresentative = Transaction("TINY CINEMA DOWNTOWN #200");
        var matching = Transaction("TINY CINEMA DOWNTOWN #300");

        var results = mapper.MapToUnresolvedTransactions(
            [
                new TransactionCategorization(
                    shortPrefixRepresentative.Id,
                    BuiltInCategoryIds.Entertainment,
                    0.70m,
                    false,
                    CategorizationSource.Ai,
                    "Short prefix.",
                    "TINY CINEMA"),
                new TransactionCategorization(
                    longPrefixRepresentative.Id,
                    BuiltInCategoryIds.RestaurantsAndCoffee,
                    0.95m,
                    false,
                    CategorizationSource.Ai,
                    "Long prefix.",
                    "TINY CINEMA DOWNTOWN")
            ],
            [shortPrefixRepresentative, longPrefixRepresentative],
            [matching]);

        results.Should().ContainSingle().Which.Should().BeEquivalentTo(new TransactionCategorization(
            matching.Id,
            BuiltInCategoryIds.RestaurantsAndCoffee,
            0.95m,
            false,
            CategorizationSource.Ai,
            "Long prefix.",
            "TINY CINEMA DOWNTOWN"));
    }

    [Fact]
    public void MapToUnresolvedTransactions_ShouldIgnoreLearnedPrefixWhenResultIsNotEligible()
    {
        var mapper = CreateMapper();
        var needsReviewRepresentative = Transaction("TINY CINEMA EAST");
        var unresolvedRepresentative = Transaction("TINY CINEMA WEST");
        var otherRepresentative = Transaction("TINY CINEMA NORTH");
        var blankPrefixRepresentative = Transaction("TINY CINEMA SOUTH");
        var nonMatchingPrefixRepresentative = Transaction("TINY CINEMA UPTOWN");
        var matching = Transaction("TINY CINEMA CENTRAL");

        var results = mapper.MapToUnresolvedTransactions(
            [
                new TransactionCategorization(
                    needsReviewRepresentative.Id,
                    BuiltInCategoryIds.Entertainment,
                    0.95m,
                    true,
                    CategorizationSource.Ai,
                    "Needs review.",
                    "TINY CINEMA"),
                new TransactionCategorization(
                    unresolvedRepresentative.Id,
                    BuiltInCategoryIds.Entertainment,
                    0.95m,
                    false,
                    CategorizationSource.Unresolved,
                    "Unresolved source.",
                    "TINY CINEMA"),
                new TransactionCategorization(
                    otherRepresentative.Id,
                    BuiltInCategoryIds.Other,
                    0.95m,
                    false,
                    CategorizationSource.Ai,
                    "Other category.",
                    "TINY CINEMA"),
                new TransactionCategorization(
                    blankPrefixRepresentative.Id,
                    BuiltInCategoryIds.Entertainment,
                    0.95m,
                    false,
                    CategorizationSource.Ai,
                    "Blank prefix.",
                    " "),
                new TransactionCategorization(
                    nonMatchingPrefixRepresentative.Id,
                    BuiltInCategoryIds.Entertainment,
                    0.95m,
                    false,
                    CategorizationSource.Ai,
                    "Non-matching prefix.",
                    "COFFEE")
            ],
            [
                needsReviewRepresentative,
                unresolvedRepresentative,
                otherRepresentative,
                blankPrefixRepresentative,
                nonMatchingPrefixRepresentative
            ],
            [matching]);

        results.Should().ContainSingle().Which.Should().BeEquivalentTo(new TransactionCategorization(
            matching.Id,
            BuiltInCategoryIds.Other,
            0m,
            true,
            CategorizationSource.Unresolved,
            "No category result was returned."));
    }

    [Fact]
    public void MapToUnresolvedTransactions_ShouldIgnoreResultsOutsideRepresentativeTransactions()
    {
        var mapper = CreateMapper();
        var representative = Transaction("KNOWN PLACE");
        var outsideTransaction = Transaction("OUTSIDE PLACE");

        var results = mapper.MapToUnresolvedTransactions(
            [
                new TransactionCategorization(
                    outsideTransaction.Id,
                    BuiltInCategoryIds.Groceries,
                    0.91m,
                    false,
                    CategorizationSource.Ai,
                    "Outside transaction.")
            ],
            [representative],
            [representative]);

        results.Should().ContainSingle().Which.Should().BeEquivalentTo(new TransactionCategorization(
            representative.Id,
            BuiltInCategoryIds.Other,
            0m,
            true,
            CategorizationSource.Unresolved,
            "No category result was returned."));
    }

    [Fact]
    public void MapToUnresolvedTransactions_ShouldRejectUnsupportedCategoryIds()
    {
        var mapper = CreateMapper();
        var transaction = Transaction("MYSTERY PLACE");

        var results = mapper.MapToUnresolvedTransactions(
            [
                new TransactionCategorization(
                    transaction.Id,
                    9999,
                    0.99m,
                    false,
                    CategorizationSource.LocalAi,
                    "Invalid test category.")
            ],
            [transaction],
            [transaction]);

        results.Should().ContainSingle().Which.Should().BeEquivalentTo(new TransactionCategorization(
            transaction.Id,
            BuiltInCategoryIds.Other,
            0m,
            true,
            CategorizationSource.Unresolved,
            "Rejected unsupported category id '9999'."));
    }

    [Fact]
    public void MapToUnresolvedTransactions_ShouldMarkUnresolvedWhenNoResultMatchesTransaction()
    {
        var mapper = CreateMapper();
        var representative = Transaction("KNOWN PLACE");
        var missing = Transaction("MYSTERY PLACE");

        var results = mapper.MapToUnresolvedTransactions(
            [
                new TransactionCategorization(
                    representative.Id,
                    BuiltInCategoryIds.Groceries,
                    0.91m,
                    false,
                    CategorizationSource.Ai,
                    "Resolved by test AI.")
            ],
            [representative],
            [representative, missing]);

        results.Should().ContainSingle(result =>
            result.TransactionId == representative.Id
            && result.CategoryId == BuiltInCategoryIds.Groceries);
        results.Should().ContainSingle(result =>
            result.TransactionId == missing.Id
            && result.CategoryId == BuiltInCategoryIds.Other
            && result.NeedsReview
            && result.Source == CategorizationSource.Unresolved
            && result.Explanation == "No category result was returned.");
    }

    private static AiCategorizationResultMapper CreateMapper()
    {
        return new AiCategorizationResultMapper(new TransactionMerchantCodeResolver());
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
