using System.Globalization;
using System.Text.RegularExpressions;

namespace Spendnest.Core.Transactions;

/// <summary>
/// Creates a stable import fingerprint for duplicate transaction detection.
/// </summary>
public static partial class TransactionFingerprint
{
    public static string Create(
        DateOnly postedDate,
        decimal amount,
        string description)
    {
        var normalizedDescription = WhitespaceRegex()
            .Replace(description.Trim().ToUpperInvariant(), " ");

        return string.Join(
            "|",
            postedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            amount.ToString("0.00", CultureInfo.InvariantCulture),
            normalizedDescription);
    }

    public static string Create(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        return Create(
            transaction.PostedDate,
            transaction.Amount,
            transaction.OriginalDescription,
            transaction.CardAccountId);
    }

    public static string Create(
        DateOnly postedDate,
        decimal amount,
        string description,
        Guid cardAccountId)
    {
        return string.Join(
            "|",
            cardAccountId,
            Create(postedDate, amount, description));
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
