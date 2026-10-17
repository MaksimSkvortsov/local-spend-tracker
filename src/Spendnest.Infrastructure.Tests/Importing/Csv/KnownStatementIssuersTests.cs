namespace Spendnest.Infrastructure.Tests.Importing.Csv;

using FluentAssertions;
using Spendnest.Infrastructure.Importing.Csv;

public class KnownStatementIssuersTests
{
    [Fact]
    public void Detect_ShouldRecognizeCapitalOneHeaders()
    {
        KnownStatementIssuers.Detect(
                ["Transaction Date", "Posted Date", "Card No.", "Description", "Category", "Debit", "Credit"])
            .Should().Be(KnownStatementIssuers.CapitalOne);
    }

    [Fact]
    public void Detect_ShouldAllowReorderedAndExtraColumns()
    {
        KnownStatementIssuers.Detect(
                ["Card No", "Posted Date", "Transaction Date", "Description", "Category", "Credit", "Debit", "Memo"])
            .Should().Be(KnownStatementIssuers.CapitalOne);
    }

    [Theory]
    [InlineData("Date", "Card Number", "Description", "Amount")]
    [InlineData("Transaction Date", "Posted Date", "Card No.", "Description", "Debit", "Credit")]
    public void Detect_ShouldReturnNullWhenNoSignatureMatchesFully(params string[] headers)
    {
        KnownStatementIssuers.Detect(headers).Should().BeNull();
    }
}
