using Spendnest.Core.Transactions;

namespace Spendnest.Infrastructure.Importing.Csv;

/// <summary>
/// A single amount column whose sign distinguishes expenses from credits.
/// </summary>
public sealed record SignedAmountColumn(
    string ColumnName,
    bool ExpensesAreNegative) : StatementAmountColumns
{
    public override bool TryRead(Func<string, string?> getField, out decimal amount)
    {
        amount = 0;

        if (!StatementAmountText.TryParse(getField(ColumnName), out var signedAmount))
        {
            return false;
        }

        amount = StatementAmountNormalizer.FromSignedStatementAmount(signedAmount, ExpensesAreNegative);
        return true;
    }
}
