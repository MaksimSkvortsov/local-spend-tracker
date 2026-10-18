using Spendnest.Core.Transactions;

namespace Spendnest.Infrastructure.Importing.Csv;

/// <summary>
/// An amount column paired with a transaction type column that says whether the row is a debit or a credit,
/// as in Capital One checking exports where every amount is positive. Rows whose type is neither
/// fall back to the amount's sign.
/// </summary>
public sealed record TypedAmountColumn(
    string AmountColumnName,
    string TypeColumnName,
    bool ExpensesAreNegative) : StatementAmountColumns
{
    private const string DebitType = "Debit";
    private const string CreditType = "Credit";

    public override bool TryRead(Func<string, string?> getField, out decimal amount)
    {
        amount = 0;

        if (!StatementAmountText.TryParse(getField(AmountColumnName), out var statementAmount))
        {
            return false;
        }

        var type = getField(TypeColumnName)?.Trim();
        if (string.Equals(type, DebitType, StringComparison.OrdinalIgnoreCase))
        {
            amount = StatementAmountNormalizer.FromDebit(statementAmount);
        }
        else if (string.Equals(type, CreditType, StringComparison.OrdinalIgnoreCase))
        {
            amount = StatementAmountNormalizer.FromCredit(statementAmount);
        }
        else
        {
            amount = StatementAmountNormalizer.FromSignedStatementAmount(statementAmount, ExpensesAreNegative);
        }

        return true;
    }
}
