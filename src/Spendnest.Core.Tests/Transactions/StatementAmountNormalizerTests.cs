namespace Spendnest.Core.Tests.Transactions;

using FluentAssertions;
using Spendnest.Core.Transactions;

public class StatementAmountNormalizerTests
{
    [Theory]
    [InlineData(-42.10, 42.10)]
    [InlineData(12.50, -12.50)]
    [InlineData(0, 0)]
    public void FromSignedStatementAmount_ShouldTreatNegativeAmountsAsExpensesByDefault(
        decimal statementAmount,
        decimal expected)
    {
        StatementAmountNormalizer.FromSignedStatementAmount(statementAmount).Should().Be(expected);
    }

    [Theory]
    [InlineData(42.10, 42.10)]
    [InlineData(-12.50, -12.50)]
    [InlineData(0, 0)]
    public void FromSignedStatementAmount_ShouldKeepSignWhenExpensesArePositive(
        decimal statementAmount,
        decimal expected)
    {
        StatementAmountNormalizer
            .FromSignedStatementAmount(statementAmount, expensesAreNegative: false)
            .Should()
            .Be(expected);
    }

    [Theory]
    [InlineData(8.40, 8.40)]
    [InlineData(-8.40, 8.40)]
    [InlineData(0, 0)]
    public void FromDebit_ShouldReturnPositiveExpense(
        decimal debitAmount,
        decimal expected)
    {
        StatementAmountNormalizer.FromDebit(debitAmount).Should().Be(expected);
    }

    [Theory]
    [InlineData(2193.82, -2193.82)]
    [InlineData(-2193.82, -2193.82)]
    [InlineData(0, 0)]
    public void FromCredit_ShouldReturnNegativeCredit(
        decimal creditAmount,
        decimal expected)
    {
        StatementAmountNormalizer.FromCredit(creditAmount).Should().Be(expected);
    }
}
