namespace Spendnest.UiHarness.Tests.Seeding;

using FluentAssertions;
using Spendnest.Core.Categories;
using Spendnest.UiHarness.Seeding;

public class HarnessSeedDataTests
{
    [Fact]
    public void Build_ShouldProduceIdenticalDataForTheSameCards()
    {
        var first = HarnessSeedData.Build(SeedCardCatalog.All);
        var second = HarnessSeedData.Build(SeedCardCatalog.All);

        second.Should().BeEquivalentTo(first, options => options.WithStrictOrdering());
    }

    [Fact]
    public void Build_WithoutCards_ShouldProduceNoDataOrRules()
    {
        var data = HarnessSeedData.Build([]);

        data.Cards.Should().BeEmpty();
        data.Imports.Should().BeEmpty();
        data.Transactions.Should().BeEmpty();
        data.Assignments.Should().BeEmpty();
        data.Rules.Should().BeEmpty();
    }

    [Fact]
    public void Build_ShouldImportDecember2025AndJanuary2026ForEachCard()
    {
        var data = HarnessSeedData.Build(SeedCardCatalog.All);

        data.Cards.Select(card => card.Name)
            .Should().Equal("Chase Sapphire", "CapOne Debit", "BofA Checking");
        data.Imports.Select(statementImport => statementImport.FileName).Should().Equal(
            "sapphire-2025-12.csv",
            "sapphire-2026-01.csv",
            "capone-2025-12.csv",
            "capone-2026-01.csv",
            "boa-2025-12.csv",
            "boa-2026-01.csv");
        data.Transactions
            .Select(transaction => (transaction.PostedDate.Year, transaction.PostedDate.Month))
            .Distinct()
            .Should().BeEquivalentTo([(2025, 12), (2026, 1)]);
        data.Rules.Should().ContainSingle(rule => rule.Pattern == "UBER");
    }

    [Fact]
    public void Build_ShouldGiveEachCardMonthARefundMoneyInAndOneReview()
    {
        var data = HarnessSeedData.Build(SeedCardCatalog.All);
        var assignments = data.Assignments.ToDictionary(assignment => assignment.TransactionId);

        foreach (var statementImport in data.Imports)
        {
            var rows = data.Transactions
                .Where(transaction => transaction.StatementImportId == statementImport.Id)
                .Select(transaction => (transaction.Amount, Assignment: assignments[transaction.Id]))
                .ToArray();

            rows.Should().Contain(
                row => row.Amount < 0 && BuiltInCategories.CountsAsSpending(row.Assignment.CategoryId),
                $"{statementImport.FileName} should have a refund");
            rows.Should().Contain(
                row => !BuiltInCategories.CountsAsSpending(row.Assignment.CategoryId),
                $"{statementImport.FileName} should have a card payment or deposit");
            rows.Should().ContainSingle(
                row => row.Assignment.NeedsReview,
                $"{statementImport.FileName} should have one transaction to review");
            statementImport.SavedTransactionCount.Should().Be(rows.Length);
        }
    }

    [Fact]
    public void ToResult_ShouldSummarizeSpendingWithoutPaymentsOrIncome()
    {
        var data = HarnessSeedData.Build([SeedCardCatalog.ChaseSapphire]);

        var result = data.ToResult(settingsConfigured: true);

        result.SettingsConfigured.Should().BeTrue();
        var imports = result.Cards.Should().ContainSingle().Subject.Imports;
        imports.Select(statementImport => statementImport.SpendingTotal).Should().Equal(659.74m, 593.77m);
        imports.Should().AllSatisfy(statementImport =>
        {
            statementImport.TransactionCount.Should().Be(10);
            statementImport.NeedsReviewCount.Should().Be(1);
        });
    }
}
