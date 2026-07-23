namespace Spendnest.Core.Transactions;

/// <summary>
/// Applies Spendnest's credit-card amount convention.
/// Expenses are positive; credits, refunds, and payments are negative.
/// </summary>
public static class StatementAmountNormalizer
{
    public static decimal FromSignedStatementAmount(
        decimal amount,
        bool expensesAreNegative = true)
    {
        if (amount == 0)
        {
            return 0;
        }

        return expensesAreNegative
            ? -amount
            : amount;
    }

    public static decimal FromDebit(decimal amount)
    {
        return Math.Abs(amount);
    }

    public static decimal FromCredit(decimal amount)
    {
        return -Math.Abs(amount);
    }
}
