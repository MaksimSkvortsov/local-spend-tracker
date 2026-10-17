namespace Spendnest.Infrastructure.Importing.Csv;

/// <summary>
/// Describes where a statement keeps transaction amounts and converts them to Spendnest's sign convention.
/// </summary>
public abstract record StatementAmountColumns
{
    /// <summary>
    /// Reads a normalized amount from the row: expenses positive, credits and payments negative.
    /// </summary>
    public abstract bool TryRead(Func<string, string?> getField, out decimal amount);
}
