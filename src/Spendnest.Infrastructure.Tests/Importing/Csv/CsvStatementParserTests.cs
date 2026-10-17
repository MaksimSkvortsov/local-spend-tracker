namespace Spendnest.Infrastructure.Tests.Importing.Csv;

using System.Text;
using FluentAssertions;
using Spendnest.Core.Importing;
using Spendnest.Infrastructure.Importing.Csv;

public class CsvStatementParserTests
{
    private readonly CsvStatementParser parser = new();

    [Fact]
    public async Task ParseAsync_ShouldParseBankOfAmericaSignedAmountStatement()
    {
        var result = await ParseFixtureAsync("bank-of-america.csv");

        result.Rows.Should().HaveCount(2);
        result.Rows[0].PostedDate.Should().Be(new DateOnly(2026, 7, 18));
        result.Rows[0].OriginalDescription.Should().Be("BULK MART #0218 RIVERTON VA");
        result.Rows[0].Amount.Should().Be(141.83m);
        result.Rows[1].OriginalDescription.Should().Be("DOORBELL SOLO PLAN EXAMPLE.COM CA");
        result.Rows[1].Amount.Should().Be(4.99m);
        result.FailedRowCount.Should().Be(0);
        result.HasTransaction.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task ParseAsync_ShouldParseCapitalOneDebitAndCreditStatement()
    {
        var result = await ParseFixtureAsync("capital-one.csv");

        result.Rows.Should().HaveCount(4);
        result.Rows.Should().OnlyContain(row => row.PostedDate == new DateOnly(2025, 12, 31));
        result.Rows[0].Amount.Should().Be(-2193.82m);
        result.Rows[0].SourceCategory.Should().Be("Payment/Credit");
        result.Rows[1].Amount.Should().Be(8.40m);
        result.Rows[1].SourceCategory.Should().Be("Dining");
        result.Rows[2].Amount.Should().Be(24.13m);
        result.Rows[2].SourceCategory.Should().Be("Merchandise");
        result.Rows[3].Amount.Should().Be(22.00m);
        result.Rows[3].SourceCategory.Should().Be("Dining");
        result.CardNumber.Should().Be("2246");
        result.CardIssuer.Should().Be("Capital One");
    }

    [Fact]
    public async Task ParseAsync_ShouldDetectCardNumberFromCardNumberColumn()
    {
        const string csv = """
            Date,Card Number,Description,Amount
            07/01/2026, 9876 ,Grocery Store,-42.10
            07/02/2026,1111,Coffee,-3.50
            """;

        var result = await ParseAsync(csv);

        result.CardNumber.Should().Be("9876");
        result.CardIssuer.Should().BeNull();
    }

    [Fact]
    public async Task ParseAsync_ShouldNotDetectCardNumberWhenColumnIsMissing()
    {
        var result = await ParseFixtureAsync("bank-of-america.csv");

        result.CardNumber.Should().BeNull();
        result.CardIssuer.Should().BeNull();
    }

    [Fact]
    public async Task ParseAsync_ShouldSkipSummarySectionBeforeTransactionHeader()
    {
        var result = await ParseFixtureAsync("bank-of-america-checking.csv");

        result.Rows.Should().HaveCount(12);
        result.Rows.Select(row => row.SourceRowNumber).Should().Equal(Enumerable.Range(9, 12));
        result.Rows[0].PostedDate.Should().Be(new DateOnly(2025, 4, 1));
        result.Rows[0].OriginalDescription.Should().StartWith("EXAMPLE CARD DES:MOBILE PMT");
        result.Rows[0].Amount.Should().Be(1240.18m);
        result.Rows[5].OriginalDescription.Should().Be("Transfer JORDAN SAMPLE");
        result.Rows[5].Amount.Should().Be(-1500.00m);
        result.Rows[11].PostedDate.Should().Be(new DateOnly(2025, 4, 15));
        result.Rows[11].Amount.Should().Be(1100.00m);
        result.Rows.Should().NotContain(row => row.OriginalDescription.Contains("balance as of", StringComparison.OrdinalIgnoreCase)
            || row.OriginalDescription.StartsWith("Total ", StringComparison.Ordinal));

        result.Warnings.Should().ContainSingle()
            .Which.Should().Be(new StatementParseWarning(8, "Amount could not be parsed."));
        result.TotalRowCount.Should().Be(13);
        result.FailedRowCount.Should().Be(1);
        result.Error.Should().BeNull();
    }

    [Theory]
    [InlineData(0, 0, 1)]
    [InlineData(0, 1, 1)]
    [InlineData(1, 0, 3)]
    [InlineData(2, 1, 0)]
    [InlineData(4, 2, 25)]
    public async Task ParseAsync_ShouldFindTransactionHeaderAfterAnyNumberOfSummaryRows(
        int summaryRowCount,
        int blankLineCount,
        int transactionCount)
    {
        var lines = new List<string>();
        if (summaryRowCount > 0)
        {
            lines.Add("Description,,Summary Amt.");
            lines.AddRange(Enumerable.Range(1, summaryRowCount).Select(index => $"Summary line {index},,\"1,000.00\""));
        }

        lines.AddRange(Enumerable.Repeat(string.Empty, blankLineCount));
        lines.Add("Date,Description,Amount,Running Bal.");
        var headerLineNumber = lines.Count;
        lines.AddRange(Enumerable.Range(1, transactionCount).Select(index => $"04/01/2025,\"Purchase {index}\",\"-{index}.00\",\"1,000.00\""));

        var result = await ParseAsync(string.Join("\r\n", lines));

        result.Warnings.Should().BeEmpty();
        result.TotalRowCount.Should().Be(transactionCount);
        result.Rows.Select(row => row.OriginalDescription)
            .Should().Equal(Enumerable.Range(1, transactionCount).Select(index => $"Purchase {index}"));
        result.Rows.Select(row => row.SourceRowNumber)
            .Should().Equal(Enumerable.Range(headerLineNumber + 1, transactionCount));
    }

    [Fact]
    public async Task ParseAsync_ShouldReportFirstRowColumnErrorsWhenNoTransactionHeaderIsFound()
    {
        const string csv = """
            Description,,Summary Amt.
            Beginning balance as of 11/14/2017,,"4,812.40"

            Ending balance as of 04/16/2025,,"1,807.02"
            """;

        var result = await ParseAsync(csv);

        result.HasTransaction.Should().BeFalse();
        result.TotalRowCount.Should().Be(0);
        result.Error.Should().Be("Date column could not be detected.");
        result.Warnings.Should().OnlyContain(warning => warning.SourceRowNumber == null);
    }

    [Fact]
    public async Task ParseAsync_ShouldCaptureOptionalSourceCategory()
    {
        const string csv = """
            Date,Description,Category,Amount
            07/01/2026,Hotel Stay,Lodging,-142.10
            """;

        var result = await ParseAsync(csv);

        result.Rows.Should().ContainSingle()
            .Which.SourceCategory.Should().Be("Lodging");
    }

    [Fact]
    public async Task ParseAsync_ShouldParseGenericSignedAmountColumn()
    {
        const string csv = """
            Date,Description,Amount
            07/01/2026,Grocery Store,-42.10
            07/02/2026,Refund,12.50
            """;

        var result = await ParseAsync(csv);

        result.Rows.Should().HaveCount(2);
        result.Rows[0].Amount.Should().Be(42.10m);
        result.Rows[1].Amount.Should().Be(-12.50m);
    }

    [Fact]
    public async Task ParseAsync_ShouldSupportQuotedDescriptionsWithCommas()
    {
        const string csv = """
            Date,Description,Amount
            07/01/2026,"Restaurant, Downtown",-18.95
            """;

        var result = await ParseAsync(csv);

        result.Rows.Should().ContainSingle();
        result.Rows[0].OriginalDescription.Should().Be("Restaurant, Downtown");
    }

    [Fact]
    public async Task ParseAsync_ShouldReturnWarningsForInvalidRows()
    {
        const string csv = """
            Date,Description,Amount
            bad date,Grocery Store,-42.10
            07/02/2026,,12.50
            07/03/2026,Valid Store,-8.25
            """;

        var result = await ParseAsync(csv);

        result.Rows.Should().ContainSingle();
        result.FailedRowCount.Should().Be(2);
        result.Warnings.Should().HaveCount(2);
        result.HasTransaction.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task ParseAsync_ShouldReturnErrorWhenNoTransactionRowsCanBeParsed()
    {
        const string csv = """
            Date,Description,Amount
            bad date,Grocery Store,-42.10
            """;

        var result = await ParseAsync(csv);

        result.HasTransaction.Should().BeFalse();
        result.Error.Should().Be("Date 'bad date' could not be parsed.");
        result.Warnings.Should().ContainSingle();
    }

    [Fact]
    public async Task ParseAsync_ShouldReturnErrorWhenRequiredColumnsAreMissing()
    {
        const string csv = """
            Description,Summary Amt.
            Ending Balance,100.00
            """;

        var result = await ParseAsync(csv);

        result.HasTransaction.Should().BeFalse();
        result.Error.Should().Be("Date column could not be detected.");
    }

    [Fact]
    public async Task ParseAsync_ShouldUseExplicitDateFormat()
    {
        const string csv = """
            Posted Date,Details,Amount
            23-07-2026,Train Ticket,-12.00
            """;

        var result = await ParseAsync(csv, new StatementParseOptions(DateFormat: "dd-MM-yyyy"));

        result.Rows.Should().ContainSingle();
        result.Rows[0].PostedDate.Should().Be(new DateOnly(2026, 7, 23));
    }

    [Fact]
    public async Task ParseAsync_ShouldLimitRowsInPreviewMode()
    {
        const string csv = """
            Date,Description,Amount
            07/01/2026,First,-1.00
            07/02/2026,Second,-2.00
            07/03/2026,Third,-3.00
            """;

        var result = await ParseAsync(csv, new StatementParseOptions(PreviewOnly: true, PreviewRowLimit: 2));

        result.Rows.Should().HaveCount(2);
        result.TotalRowCount.Should().Be(3);
    }

    private Task<StatementParseResult> ParseAsync(
        string csv,
        StatementParseOptions? options = null)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        return parser.ParseAsync(stream, options ?? new StatementParseOptions(), CancellationToken.None);
    }

    private async Task<StatementParseResult> ParseFixtureAsync(
        string fileName,
        StatementParseOptions? options = null)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Csv", fileName);
        await using var stream = File.OpenRead(path);
        return await parser.ParseAsync(stream, options ?? new StatementParseOptions(), CancellationToken.None);
    }
}
