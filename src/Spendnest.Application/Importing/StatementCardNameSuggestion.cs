using System.Text.RegularExpressions;

namespace Spendnest.Application.Importing;

/// <summary>
/// Suggests a card account name for a statement from its detected card number or file name.
/// </summary>
public static partial class StatementCardNameSuggestion
{
    private const string DefaultCardName = "Imported Card";

    /// <summary>
    /// Returns a card name for a detected card number, prefixed with the issuer when it was recognized,
    /// or null when no card number was detected.
    /// </summary>
    public static string? FromCardNumber(string? cardNumber, string? cardIssuer)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            return null;
        }

        var issuer = string.IsNullOrWhiteSpace(cardIssuer) ? "Card" : cardIssuer.Trim();
        return $"{issuer} {cardNumber.Trim()}";
    }

    public static string FromFileName(string? fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(stem))
        {
            return DefaultCardName;
        }

        var normalized = SeparatorRegex()
            .Replace(stem, " ")
            .Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? DefaultCardName
            : normalized;
    }

    [GeneratedRegex(@"[_\-]+")]
    private static partial Regex SeparatorRegex();
}
