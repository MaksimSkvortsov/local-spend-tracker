using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using Spendnest.Core.Importing;
using Spendnest.Core.Transactions;

namespace Spendnest.Infrastructure.Importing;

/// <summary>
/// Parses comma-separated credit-card statement files using CsvHelper.
/// </summary>
public sealed class CsvStatementParser : IStatementParser
{
    private static readonly string[] DateFormats =
    [
        "M/d/yyyy",
        "MM/dd/yyyy",
        "M/d/yy",
        "MM/dd/yy",
        "yyyy-MM-dd",
        "M-d-yyyy",
        "MM-dd-yyyy",
        "MMM d, yyyy",
        "MMMM d, yyyy"
    ];

    private static readonly string[] DateHeaders = ["Posted Date", "Post Date", "Date", "Transaction Date"];
    private static readonly string[] DescriptionHeaders = ["Description", "Payee", "Merchant", "Details", "Transaction Description"];
    private static readonly string[] AmountHeaders = ["Amount", "Transaction Amount"];
    private static readonly string[] DebitHeaders = ["Debit", "Debits", "Withdrawal", "Withdrawals", "Charge"];
    private static readonly string[] CreditHeaders = ["Credit", "Credits", "Deposit", "Deposits", "Payment"];

    public async Task<StatementParseResult> ParseAsync(
        Stream stream,
        StatementParseOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(options);

        using var reader = new StreamReader(stream, leaveOpen: true);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            BadDataFound = null,
            HasHeaderRecord = true,
            MissingFieldFound = null,
            TrimOptions = TrimOptions.Trim
        });

        if (!await csv.ReadAsync().ConfigureAwait(false))
        {
            return new StatementParseResult([], [new StatementParseWarning(null, "CSV file is empty.")], 0, 0);
        }

        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? [];
        var mappings = ResolveMappings(headers, options);
        var warnings = new List<StatementParseWarning>();

        ValidateMappings(mappings, warnings);

        if (warnings.Count > 0)
        {
            return new StatementParseResult([], warnings, 0, 0);
        }

        var rows = new List<ParsedStatementRow>();
        var totalRows = 0;
        var failedRows = 0;

        while (await csv.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            totalRows++;
            var sourceRowNumber = csv.Parser.Row;

            if (options.PreviewOnly && options.PreviewRowLimit is not null && rows.Count >= options.PreviewRowLimit.Value)
            {
                continue;
            }

            var parseResult = TryParseRow(csv, mappings, options, sourceRowNumber);
            if (parseResult.Row is not null)
            {
                rows.Add(parseResult.Row);
                continue;
            }

            failedRows++;
            warnings.Add(new StatementParseWarning(sourceRowNumber, parseResult.ErrorMessage ?? "Row could not be parsed."));
        }

        return new StatementParseResult(rows, warnings, totalRows, failedRows);
    }

    private static StatementColumnMappings ResolveMappings(
        IReadOnlyList<string> headers,
        StatementParseOptions options)
    {
        return new StatementColumnMappings(
            FindHeader(headers, options.DateColumnName, DateHeaders),
            FindHeader(headers, options.DescriptionColumnName, DescriptionHeaders),
            FindHeader(headers, options.AmountColumnName, AmountHeaders),
            FindHeader(headers, options.DebitColumnName, DebitHeaders),
            FindHeader(headers, options.CreditColumnName, CreditHeaders));
    }

    private static void ValidateMappings(
        StatementColumnMappings mappings,
        List<StatementParseWarning> warnings)
    {
        if (mappings.DateColumnName is null)
        {
            warnings.Add(new StatementParseWarning(null, "Date column could not be detected."));
        }

        if (mappings.DescriptionColumnName is null)
        {
            warnings.Add(new StatementParseWarning(null, "Description column could not be detected."));
        }

        if (mappings.AmountColumnName is null && (mappings.DebitColumnName is null || mappings.CreditColumnName is null))
        {
            warnings.Add(new StatementParseWarning(null, "Amount column or debit/credit columns could not be detected."));
        }
    }

    private static RowParseResult TryParseRow(
        CsvReader csv,
        StatementColumnMappings mappings,
        StatementParseOptions options,
        int sourceRowNumber)
    {
        var dateText = GetField(csv, mappings.DateColumnName);
        var description = GetField(csv, mappings.DescriptionColumnName);

        if (string.IsNullOrWhiteSpace(dateText) && string.IsNullOrWhiteSpace(description))
        {
            return RowParseResult.Failed("Row is empty.");
        }

        if (!TryParseDate(dateText, options.DateFormat, out var postedDate))
        {
            return RowParseResult.Failed($"Date '{dateText}' could not be parsed.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return RowParseResult.Failed("Description is missing.");
        }

        if (!TryParseAmount(csv, mappings, options, out var amount))
        {
            return RowParseResult.Failed("Amount could not be parsed.");
        }

        return RowParseResult.Succeeded(new ParsedStatementRow(
            postedDate,
            description.Trim(),
            amount,
            sourceRowNumber));
    }

    private static bool TryParseDate(
        string? value,
        string? dateFormat,
        out DateOnly date)
    {
        date = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(dateFormat))
        {
            return DateOnly.TryParseExact(value.Trim(), dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        return DateOnly.TryParseExact(value.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
            || DateOnly.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static bool TryParseAmount(
        CsvReader csv,
        StatementColumnMappings mappings,
        StatementParseOptions options,
        out decimal amount)
    {
        amount = 0;

        if (mappings.AmountColumnName is not null)
        {
            var amountText = GetField(csv, mappings.AmountColumnName);
            if (!TryParseDecimal(amountText, out var signedAmount))
            {
                return false;
            }

            amount = StatementAmountNormalizer.FromSignedStatementAmount(signedAmount, options.SignedAmountExpensesAreNegative);
            return true;
        }

        var debitText = GetField(csv, mappings.DebitColumnName);
        var creditText = GetField(csv, mappings.CreditColumnName);

        if (TryParseDecimal(debitText, out var debitAmount) && debitAmount != 0)
        {
            amount = StatementAmountNormalizer.FromDebit(debitAmount);
            return true;
        }

        if (TryParseDecimal(creditText, out var creditAmount) && creditAmount != 0)
        {
            amount = StatementAmountNormalizer.FromCredit(creditAmount);
            return true;
        }

        return false;
    }

    private static bool TryParseDecimal(string? value, out decimal amount)
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

    private static string? FindHeader(
        IReadOnlyList<string> headers,
        string? explicitHeader,
        IReadOnlyList<string> candidates)
    {
        if (!string.IsNullOrWhiteSpace(explicitHeader))
        {
            return headers.FirstOrDefault(header => HeaderEquals(header, explicitHeader));
        }

        foreach (var candidate in candidates)
        {
            var match = headers.FirstOrDefault(header => HeaderEquals(header, candidate));
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static string? GetField(CsvReader csv, string? columnName)
    {
        return columnName is null ? null : csv.GetField(columnName);
    }

    private static bool HeaderEquals(string left, string right)
    {
        return NormalizeHeader(left) == NormalizeHeader(right);
    }

    private static string NormalizeHeader(string value)
    {
        return Regex.Replace(value, "[^A-Za-z0-9]", string.Empty).ToUpperInvariant();
    }

    private sealed record StatementColumnMappings(
        string? DateColumnName,
        string? DescriptionColumnName,
        string? AmountColumnName,
        string? DebitColumnName,
        string? CreditColumnName);

    private sealed record RowParseResult(
        ParsedStatementRow? Row,
        string? ErrorMessage)
    {
        public static RowParseResult Succeeded(ParsedStatementRow row)
        {
            return new RowParseResult(row, null);
        }

        public static RowParseResult Failed(string message)
        {
            return new RowParseResult(null, message);
        }
    }
}
