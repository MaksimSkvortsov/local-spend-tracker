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

        var description = NormalizeDescription(transaction.OriginalDescription);
        var segment = SelectPreferredSegment(description);
        var normalizedSegment = NormalizeSegment(segment);
        var tokens = Tokenize(normalizedSegment);
        RemoveTrailingNoise(tokens);

        return BuildMerchantCode(tokens, normalizedSegment);
    }

    private static string NormalizeDescription(string description)
    {
        return description.Trim().ToUpperInvariant();
    }

    private static string SelectPreferredSegment(string description)
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

    private static string NormalizeSegment(string segment)
    {
        return NonAlphaNumericRegex().Replace(segment, " ");
    }

    private static List<string> Tokenize(string normalizedSegment)
    {
        return WhitespaceRegex()
            .Split(normalizedSegment.Trim())
            .Where(token => token.Length > 0)
            .ToList();
    }

    private static void RemoveTrailingNoise(List<string> tokens)
    {
        while (tokens.Count > 0 && IsTrailingNoise(tokens[^1]))
        {
            tokens.RemoveAt(tokens.Count - 1);
        }
    }

    private static bool IsTrailingNoise(string token)
    {
        return token.All(char.IsDigit)
            || token.Length == 1
            || IgnoredTrailingTokens.Contains(token);
    }

    private static string BuildMerchantCode(
        IReadOnlyList<string> tokens,
        string normalizedSegment)
    {
        return tokens.Count == 0
            ? normalizedSegment.Trim()
            : string.Join(" ", tokens);
    }

    [GeneratedRegex("[^A-Z0-9]+")]
    private static partial Regex NonAlphaNumericRegex();

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();
}
