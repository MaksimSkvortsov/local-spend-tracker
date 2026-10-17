namespace Spendnest.Infrastructure.Importing.Csv;

/// <summary>
/// Recognizes card issuers whose CSV exports have a distinctive header row.
/// </summary>
public static class KnownStatementIssuers
{
    public const string CapitalOne = "Capital One";

    private static readonly IReadOnlyList<(string Issuer, string[] Headers)> Signatures =
    [
        (CapitalOne, ["Transaction Date", "Posted Date", "Card No.", "Description", "Category", "Debit", "Credit"])
    ];

    /// <summary>
    /// Returns the issuer whose signature headers are all present, in any order, or null when none match.
    /// Extra columns are allowed so an issuer adding a column does not break recognition.
    /// </summary>
    public static string? Detect(IReadOnlyList<string> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        foreach (var (issuer, signatureHeaders) in Signatures)
        {
            var hasAllHeaders = signatureHeaders.All(signatureHeader =>
                headers.Any(header => StatementHeaderName.Matches(header, signatureHeader)));
            if (hasAllHeaders)
            {
                return issuer;
            }
        }

        return null;
    }
}
