namespace Spendnest.Infrastructure.Tests.Importing.Csv;

using FluentAssertions;
using Spendnest.Infrastructure.Importing.Csv;

public class StatementAmountColumnsTests
{
    [Theory]
    [InlineData("-42.10", true, 42.10)]
    [InlineData("12.50", true, -12.50)]
    [InlineData("42.10", false, 42.10)]
    [InlineData("-12.50", false, -12.50)]
    public void SignedAmountColumn_ShouldNormalizeBySignConvention(
        string text,
        bool expensesAreNegative,
        decimal expected)
    {
        var columns = new SignedAmountColumn("Amount", expensesAreNegative);

        columns.TryRead(Field("Amount", text), out var amount).Should().BeTrue();
        amount.Should().Be(expected);
    }

    [Fact]
    public void SignedAmountColumn_ShouldFailWhenAmountIsBlank()
    {
        var columns = new SignedAmountColumn("Amount", ExpensesAreNegative: true);

        columns.TryRead(Field("Amount", ""), out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("8.40", "", 8.40)]
    [InlineData("", "2193.82", -2193.82)]
    [InlineData("0", "5.00", -5.00)]
    [InlineData("3.00", "5.00", 3.00)]
    public void DebitCreditColumns_ShouldUseNonZeroDebitBeforeCredit(
        string debit,
        string credit,
        decimal expected)
    {
        var columns = new DebitCreditColumns("Debit", "Credit");

        columns.TryRead(Fields(debit, credit), out var amount).Should().BeTrue();
        amount.Should().Be(expected);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("0", "0.00")]
    public void DebitCreditColumns_ShouldFailWhenNeitherColumnHasAnAmount(string debit, string credit)
    {
        var columns = new DebitCreditColumns("Debit", "Credit");

        columns.TryRead(Fields(debit, credit), out _).Should().BeFalse();
    }

    private static Func<string, string?> Field(string column, string value)
    {
        return name => name == column ? value : null;
    }

    private static Func<string, string?> Fields(string debit, string credit)
    {
        return name => name switch
        {
            "Debit" => debit,
            "Credit" => credit,
            _ => null
        };
    }
}
