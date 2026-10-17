using System.Globalization;

namespace Spendnest.Infrastructure.Importing.Csv;

/// <summary>
/// Reads money values as banks write them in statement files, such as <c>$1,234.56</c> or <c>(12.00)</c>.
/// </summary>
public static class StatementAmountText
{
    public static bool TryParse(string? value, out decimal amount)
    {
        amount = 0;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();
        var isParenthesized = normalized.StartsWith('(') && normalized.EndsWith(')');

        normalized = normalized
            .Replace("$", string.Empty, StringComparison.Ordinal)
            .Replace(",", string.Empty, StringComparison.Ordinal)
            .Replace("(", string.Empty, StringComparison.Ordinal)
            .Replace(")", string.Empty, StringComparison.Ordinal)
            .Trim();

        if (!decimal.TryParse(normalized, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out amount))
        {
            return false;
        }

        if (isParenthesized)
        {
            amount = -amount;
        }

        return true;
    }
}
