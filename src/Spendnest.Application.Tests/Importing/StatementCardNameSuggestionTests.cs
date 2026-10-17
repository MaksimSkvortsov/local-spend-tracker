namespace Spendnest.Application.Tests.Importing;

using FluentAssertions;
using Spendnest.Application.Importing;

public class StatementCardNameSuggestionTests
{
    [Fact]
    public void FromCardNumber_ShouldPrefixRecognizedIssuer()
    {
        StatementCardNameSuggestion.FromCardNumber(" 2246 ", "Capital One").Should().Be("Capital One 2246");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void FromCardNumber_ShouldUseGenericPrefixWhenIssuerIsUnknown(string? cardIssuer)
    {
        StatementCardNameSuggestion.FromCardNumber("1234", cardIssuer).Should().Be("Card 1234");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromCardNumber_ShouldReturnNullWhenNoNumberWasDetected(string? cardNumber)
    {
        StatementCardNameSuggestion.FromCardNumber(cardNumber, "Capital One").Should().BeNull();
    }

    [Theory]
    [InlineData("chase_sapphire-2026.csv", "chase sapphire 2026")]
    [InlineData("amex__gold--card.csv", "amex gold card")]
    [InlineData("Visa Card.csv", "Visa Card")]
    public void FromFileName_ShouldReplaceSeparatorsWithSpaces(string fileName, string expected)
    {
        StatementCardNameSuggestion.FromFileName(fileName).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(".csv")]
    [InlineData("_-_.csv")]
    public void FromFileName_ShouldUseDefaultWhenNoNameRemains(string? fileName)
    {
        StatementCardNameSuggestion.FromFileName(fileName).Should().Be("Imported Card");
    }
}
