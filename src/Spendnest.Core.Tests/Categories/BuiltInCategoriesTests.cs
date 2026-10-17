namespace Spendnest.Core.Tests.Categories;

using FluentAssertions;
using Spendnest.Core.Categories;

public class BuiltInCategoriesTests
{
    [Fact]
    public void All_ShouldContainExpectedMvpCategories()
    {
        var ids = BuiltInCategories.All.Select(category => category.Id);

        ids.Should().Equal(
            BuiltInCategoryIds.Groceries,
            BuiltInCategoryIds.RestaurantsAndCoffee,
            BuiltInCategoryIds.Transportation,
            BuiltInCategoryIds.Shopping,
            BuiltInCategoryIds.Entertainment,
            BuiltInCategoryIds.Travel,
            BuiltInCategoryIds.Healthcare,
            BuiltInCategoryIds.Utilities,
            BuiltInCategoryIds.Subscriptions,
            BuiltInCategoryIds.Insurance,
            BuiltInCategoryIds.PersonalCare,
            BuiltInCategoryIds.FeesAndCharges,
            BuiltInCategoryIds.CreditCardPayment,
            BuiltInCategoryIds.OtherCardPayment,
            BuiltInCategoryIds.IncomeAndTransfers,
            BuiltInCategoryIds.Other);
    }

    [Fact]
    public void AiSelectable_ShouldExcludeOtherCardPayment()
    {
        BuiltInCategories.AiSelectable
            .Select(category => category.Id)
            .Should()
            .NotContain(BuiltInCategoryIds.OtherCardPayment)
            .And.Contain(BuiltInCategoryIds.CreditCardPayment)
            .And.Contain(BuiltInCategoryIds.IncomeAndTransfers);
    }

    [Fact]
    public void CountsAsSpending_ShouldExcludeCardPaymentsAndIncome()
    {
        BuiltInCategories.CountsAsSpending(BuiltInCategoryIds.CreditCardPayment).Should().BeFalse();
        BuiltInCategories.CountsAsSpending(BuiltInCategoryIds.IncomeAndTransfers).Should().BeFalse();
        BuiltInCategories.CountsAsSpending(BuiltInCategoryIds.OtherCardPayment).Should().BeTrue();
        BuiltInCategories.CountsAsSpending(BuiltInCategoryIds.Other).Should().BeTrue();
    }

    [Fact]
    public void All_ShouldNotContainRefundCategory()
    {
        BuiltInCategories.All
            .Select(category => category.Name)
            .Should()
            .NotContain("Refund");
    }

    [Fact]
    public void All_ShouldHaveStableUniqueIds()
    {
        var ids = BuiltInCategories.All.Select(category => category.Id).ToArray();

        ids.Should().OnlyHaveUniqueItems();
        ids.Should().AllSatisfy(id => id.Should().BePositive());
    }

    [Fact]
    public void All_ShouldHaveUniqueSortOrder()
    {
        BuiltInCategories.All
            .Select(category => category.SortOrder)
            .Should()
            .OnlyHaveUniqueItems();
    }

    [Fact]
    public void All_ShouldHaveValidDefaultColors()
    {
        BuiltInCategories.All
            .Select(category => category.ColorHex)
            .Should()
            .AllSatisfy(color =>
            {
                color.Should().StartWith("#");
                color.Should().HaveLength(7);
            });
    }
}
