namespace Spendnest.Infrastructure.Tests.Importing.Csv;

using FluentAssertions;
using Spendnest.Infrastructure.Importing.Csv;

public class StatementValueParsingTests
{
    [Theory]
    [InlineData("12.34", 12.34)]
    [InlineData("-12.34", -12.34)]
    [InlineData("$1,234.56", 1234.56)]
    [InlineData("(12.00)", -12.00)]
    [InlineData(" ($1,000.00) ", -1000.00)]
    public void StatementAmountText_ShouldParseBankAmountFormats(string text, decimal expected)
    {
        StatementAmountText.TryParse(text, out var amount).Should().BeTrue();
        amount.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("n/a")]
    public void StatementAmountText_ShouldRejectMissingOrNonNumericValues(string? text)
    {
        StatementAmountText.TryParse(text, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("7/1/2026")]
    [InlineData("07/01/26")]
    [InlineData("2026-07-01")]
    [InlineData("Jul 1, 2026")]
    [InlineData("July 1, 2026")]
    public void StatementDateFormat_ShouldParseCommonBankFormats(string text)
    {
        new StatementDateFormat().TryParse(text, out var date).Should().BeTrue();
        date.Should().Be(new DateOnly(2026, 7, 1));
    }

    [Fact]
    public void StatementDateFormat_ShouldOnlyAcceptExplicitFormatWhenGiven()
    {
        var format = new StatementDateFormat("dd-MM-yyyy");

        format.TryParse("23-07-2026", out var date).Should().BeTrue();
        date.Should().Be(new DateOnly(2026, 7, 23));
        format.TryParse("07/23/2026", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    [InlineData("not a date")]
    public void StatementDateFormat_ShouldRejectMissingOrInvalidDates(string? text)
    {
        new StatementDateFormat().TryParse(text, out _).Should().BeFalse();
    }
}
