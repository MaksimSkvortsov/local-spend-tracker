namespace Spendnest.Infrastructure.Tests.Importing.Csv;

using FluentAssertions;
using Spendnest.Core.Importing;
using Spendnest.Infrastructure.Importing.Csv;

public class StatementColumnLayoutTests
{
    [Fact]
    public void TryDetect_ShouldPreferSignedAmountColumnOverDebitAndCredit()
    {
        var detected = StatementColumnLayout.TryDetect(
            ["Posted Date", "Payee", "Amount", "Debit", "Credit"],
            new StatementParseOptions(),
            out var layout,
            out _);

        detected.Should().BeTrue();
        layout!.DateColumnName.Should().Be("Posted Date");
        layout.DescriptionColumnName.Should().Be("Payee");
        layout.AmountColumns.Should().Be(new SignedAmountColumn("Amount", ExpensesAreNegative: true));
    }

    [Fact]
    public void TryDetect_ShouldUseDebitAndCreditColumnsWhenNoAmountColumnExists()
    {
        var detected = StatementColumnLayout.TryDetect(
            ["Transaction Date", "Posted Date", "Card No.", "Description", "Category", "Debit", "Credit"],
            new StatementParseOptions(),
            out var layout,
            out _);

        detected.Should().BeTrue();
        layout!.DateColumnName.Should().Be("Posted Date");
        layout.CategoryColumnName.Should().Be("Category");
        layout.CardNumberColumnName.Should().Be("Card No.");
        layout.AmountColumns.Should().Be(new DebitCreditColumns("Debit", "Credit"));
    }

    [Fact]
    public void TryDetect_ShouldMatchHeadersIgnoringCaseAndPunctuation()
    {
        var detected = StatementColumnLayout.TryDetect(
            ["posted_date", "DESCRIPTION", "card number", "amount"],
            new StatementParseOptions(),
            out var layout,
            out _);

        detected.Should().BeTrue();
        layout!.DateColumnName.Should().Be("posted_date");
        layout.CardNumberColumnName.Should().Be("card number");
    }

    [Fact]
    public void TryDetect_ShouldOnlyUseExplicitColumnNamesWhenGiven()
    {
        var detected = StatementColumnLayout.TryDetect(
            ["Date", "Booked", "Description", "Amount"],
            new StatementParseOptions(DateColumnName: "Booked"),
            out var layout,
            out _);

        detected.Should().BeTrue();
        layout!.DateColumnName.Should().Be("Booked");
    }

    [Fact]
    public void TryDetect_ShouldReportEveryMissingRequiredColumn()
    {
        var detected = StatementColumnLayout.TryDetect(
            ["Reference", "Debit"],
            new StatementParseOptions(),
            out var layout,
            out var errors);

        detected.Should().BeFalse();
        layout.Should().BeNull();
        errors.Should().Equal(
            "Date column could not be detected.",
            "Description column could not be detected.",
            "Amount column or debit/credit columns could not be detected.");
    }

    [Fact]
    public void TryReadRow_ShouldReturnTrimmedTransaction()
    {
        var layout = DetectLayout("Date", "Description", "Category", "Amount");

        var read = layout.TryReadRow(
            Row(("Date", "07/01/2026"), ("Description", " Hotel Stay "), ("Category", " Lodging "), ("Amount", "-142.10")),
            sourceRowNumber: 4,
            out var row,
            out var error);

        read.Should().BeTrue();
        error.Should().BeNull();
        row.Should().Be(new ParsedStatementRow(new DateOnly(2026, 7, 1), "Hotel Stay", 142.10m, 4, "Lodging"));
    }

    [Theory]
    [InlineData("", "", "-1.00", "Row is empty.")]
    [InlineData("bad date", "Store", "-1.00", "Date 'bad date' could not be parsed.")]
    [InlineData("07/01/2026", "", "-1.00", "Description is missing.")]
    [InlineData("07/01/2026", "Store", "", "Amount could not be parsed.")]
    public void TryReadRow_ShouldExplainWhyRowIsInvalid(
        string date,
        string description,
        string amount,
        string expectedError)
    {
        var layout = DetectLayout("Date", "Description", "Amount");

        var read = layout.TryReadRow(
            Row(("Date", date), ("Description", description), ("Amount", amount)),
            sourceRowNumber: 2,
            out var row,
            out var error);

        read.Should().BeFalse();
        row.Should().BeNull();
        error.Should().Be(expectedError);
    }

    [Fact]
    public void ReadCardNumber_ShouldReturnTrimmedValueOrNull()
    {
        var withCardColumn = DetectLayout("Date", "Card No.", "Description", "Amount");
        var withoutCardColumn = DetectLayout("Date", "Description", "Amount");

        withCardColumn.ReadCardNumber(Row(("Card No.", " 2246 "))).Should().Be("2246");
        withCardColumn.ReadCardNumber(Row(("Card No.", " "))).Should().BeNull();
        withoutCardColumn.ReadCardNumber(Row(("Card No.", "2246"))).Should().BeNull();
    }

    private static StatementColumnLayout DetectLayout(params string[] headers)
    {
        StatementColumnLayout.TryDetect(headers, new StatementParseOptions(), out var layout, out _)
            .Should().BeTrue();
        return layout!;
    }

    private static Func<string, string?> Row(params (string Column, string Value)[] fields)
    {
        var values = fields.ToDictionary(field => field.Column, field => field.Value);
        return column => values.GetValueOrDefault(column);
    }
}
