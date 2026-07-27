using System.Text.RegularExpressions;
using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;

namespace Spendnest.Infrastructure.Categorization;

/// <summary>
/// Normalizes statement descriptions into merchant codes used by learned rules.
/// </summary>
public sealed partial class TransactionMerchantCodeResolver : ITransactionMerchantCodeResolver
{
    private static readonly HashSet<string> IgnoredTrailingTokens = new(StringComparer.Ordinal)
    {
        "CREDIT",
        "DEBIT",
        "REFUND"
    };

    public string Resolve(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        var description = transaction.OriginalDescription.Trim().ToUpperInvariant();
        var preferredSegment = PreferredSegment(description);
        var normalized = NonAlphaNumericRegex().Replace(preferredSegment, " ");
        var tokens = WhitespaceRegex()
            .Split(normalized.Trim())
            .Where(token => token.Length > 0)
            .ToList();

        while (tokens.Count > 0
            && (tokens[^1].All(char.IsDigit)
                || tokens[^1].Length == 1
                || IgnoredTrailingTokens.Contains(tokens[^1])))
        {
            tokens.RemoveAt(tokens.Count - 1);
        }

        return tokens.Count == 0
            ? normalized.Trim()
            : string.Join(" ", tokens);
    }

    private static string PreferredSegment(string description)
    {
        var starIndex = description.IndexOf('*', StringComparison.Ordinal);
        if (starIndex > 0)
        {
            return description[..starIndex];
        }

        var hashIndex = description.IndexOf('#', StringComparison.Ordinal);
        if (hashIndex > 0)
        {
            return description[..hashIndex];
        }

        return description;
    }

    [GeneratedRegex("[^A-Z0-9]+")]
    private static partial Regex NonAlphaNumericRegex();

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();
}
