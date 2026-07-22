namespace Spendnest.Core.Tests.Categories;

using FluentAssertions;
using Spendnest.Core.Categories;

public class BuiltInCategoriesTests
{
    [Fact]
    public void All_ShouldContainExpectedMvpCategories()
    {
        var codes = BuiltInCategories.All.Select(category => category.Code);

        codes.Should().Equal(
            BuiltInCategoryCodes.Groceries,
            BuiltInCategoryCodes.RestaurantsAndCoffee,
            BuiltInCategoryCodes.Transportation,
            BuiltInCategoryCodes.Shopping,
            BuiltInCategoryCodes.Entertainment,
            BuiltInCategoryCodes.Travel,
            BuiltInCategoryCodes.Healthcare,
            BuiltInCategoryCodes.Utilities,
            BuiltInCategoryCodes.Subscriptions,
            BuiltInCategoryCodes.Insurance,
            BuiltInCategoryCodes.PersonalCare,
            BuiltInCategoryCodes.FeesAndCharges,
            BuiltInCategoryCodes.CreditCardPayment,
            BuiltInCategoryCodes.Refund,
            BuiltInCategoryCodes.Other);
    }

    [Fact]
    public void All_ShouldHaveStableUniqueCodes()
    {
        var codes = BuiltInCategories.All.Select(category => category.Code).ToArray();

        codes.Should().OnlyHaveUniqueItems();
        codes.Should().AllSatisfy(code => code.Should().NotBeNullOrWhiteSpace());
    }

    [Fact]
    public void All_ShouldHaveUniqueSortOrder()
    {
        BuiltInCategories.All
            .Select(category => category.SortOrder)
            .Should()
            .OnlyHaveUniqueItems();
    }
}
