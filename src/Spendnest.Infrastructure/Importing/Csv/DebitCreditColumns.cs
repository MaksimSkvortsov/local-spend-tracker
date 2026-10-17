using Spendnest.Core.Transactions;

namespace Spendnest.Infrastructure.Importing.Csv;

/// <summary>
/// Separate debit and credit columns; a row uses whichever has a non-zero value, preferring debit.
/// </summary>
public sealed record DebitCreditColumns(
    string DebitColumnName,
    string CreditColumnName) : StatementAmountColumns
{
    public override bool TryRead(Func<string, string?> getField, out decimal amount)
    {
        amount = 0;

        if (StatementAmountText.TryParse(getField(DebitColumnName), out var debitAmount) && debitAmount != 0)
        {
            amount = StatementAmountNormalizer.FromDebit(debitAmount);
            return true;
        }

        if (StatementAmountText.TryParse(getField(CreditColumnName), out var creditAmount) && creditAmount != 0)
        {
            amount = StatementAmountNormalizer.FromCredit(creditAmount);
            return true;
        }

        return false;
    }
}
