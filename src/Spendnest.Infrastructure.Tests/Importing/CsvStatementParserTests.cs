namespace Spendnest.Infrastructure.Tests.Importing;

using System.Text;
using FluentAssertions;
using Spendnest.Core.Importing;
using Spendnest.Infrastructure.Importing;

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
    }

    [Fact]
    public async Task ParseAsync_ShouldParseCapitalOneDebitAndCreditStatement()
    {
        var result = await ParseFixtureAsync("capital-one.csv");

        result.Rows.Should().HaveCount(4);
        result.Rows.Should().OnlyContain(row => row.PostedDate == new DateOnly(2025, 12, 31));
        result.Rows[0].Amount.Should().Be(-2193.82m);
        result.Rows[1].Amount.Should().Be(8.40m);
        result.Rows[2].Amount.Should().Be(24.13m);
        result.Rows[3].Amount.Should().Be(22.00m);
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
