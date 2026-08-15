namespace Spendnest.Infrastructure.Tests.Categorization;

using FluentAssertions;
using Spendnest.Core.Categories;
using Spendnest.Infrastructure.Categorization;

public class OpenAiCategoryGuidanceTests
{
    [Fact]
    public void Get_ShouldReturnPromptGuidanceForBuiltInCategories()
    {
        OpenAiCategoryGuidance.Get(BuiltInCategoryIds.Groceries).Should().Contain("Grocery stores");
        OpenAiCategoryGuidance.Get(BuiltInCategoryIds.CreditCardPayment).Should().Contain("Credit-card payments");
        OpenAiCategoryGuidance.Get(BuiltInCategoryIds.Other).Should().Contain("Only for transactions");
    }

    [Fact]
    public void Get_ShouldReturnEmptyGuidanceForUnknownCategoryIds()
    {
        OpenAiCategoryGuidance.Get(9999).Should().BeEmpty();
    }
}
