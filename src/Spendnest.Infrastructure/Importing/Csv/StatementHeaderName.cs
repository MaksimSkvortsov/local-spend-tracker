using System.Text.RegularExpressions;

namespace Spendnest.Infrastructure.Importing.Csv;

/// <summary>
/// Compares statement header names the way banks vary them: ignoring case, spaces, and punctuation.
/// </summary>
public static partial class StatementHeaderName
{
    public static bool Matches(string left, string right)
    {
        return Normalize(left) == Normalize(right);
    }

    private static string Normalize(string value)
    {
        return NonAlphanumericRegex().Replace(value, string.Empty).ToUpperInvariant();
    }

    [GeneratedRegex("[^A-Za-z0-9]")]
    private static partial Regex NonAlphanumericRegex();
}
