using System.Diagnostics.CodeAnalysis;
using Spendnest.Core.Importing;

namespace Spendnest.Infrastructure.Importing.Csv;

/// <summary>
/// The columns a statement file uses for transaction data, detected from its header row,
/// and the rules for turning one data row into a parsed transaction.
/// </summary>
public sealed class StatementColumnLayout
{
    private static readonly string[] DateHeaders = ["Posted Date", "Post Date", "Date", "Transaction Date"];
    private static readonly string[] DescriptionHeaders = ["Description", "Payee", "Merchant", "Details", "Transaction Description"];
    private static readonly string[] CategoryHeaders = ["Category", "Transaction Category"];
    private static readonly string[] AmountHeaders = ["Amount", "Transaction Amount"];
    private static readonly string[] DebitHeaders = ["Debit", "Debits", "Withdrawal", "Withdrawals", "Charge"];
    private static readonly string[] CreditHeaders = ["Credit", "Credits", "Deposit", "Deposits", "Payment"];
    private static readonly string[] CardNumberHeaders = ["Card No.", "Card Number"];

    private StatementColumnLayout(
        string dateColumnName,
        string descriptionColumnName,
        string? categoryColumnName,
        string? cardNumberColumnName,
        string? cardIssuer,
        StatementAmountColumns amountColumns,
        StatementDateFormat dateFormat)
    {
        DateColumnName = dateColumnName;
        DescriptionColumnName = descriptionColumnName;
        CategoryColumnName = categoryColumnName;
        CardNumberColumnName = cardNumberColumnName;
        CardIssuer = cardIssuer;
        AmountColumns = amountColumns;
        DateFormat = dateFormat;
    }

    public string DateColumnName { get; }

    public string DescriptionColumnName { get; }

    public string? CategoryColumnName { get; }

    public string? CardNumberColumnName { get; }

    /// <summary>
    /// The card issuer recognized from the header row, or null when the layout is not a known issuer's export.
    /// </summary>
    public string? CardIssuer { get; }

    public StatementAmountColumns AmountColumns { get; }

    public StatementDateFormat DateFormat { get; }

    /// <summary>
    /// Detects the layout from a header row. Explicit column names in <paramref name="options"/> replace
    /// the built-in header candidates. Returns false with one error per missing required column.
    /// </summary>
    public static bool TryDetect(
        IReadOnlyList<string> headers,
        StatementParseOptions options,
        [NotNullWhen(true)] out StatementColumnLayout? layout,
        out IReadOnlyList<string> errors)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(options);

        var dateColumnName = FindHeader(headers, options.DateColumnName, DateHeaders);
        var descriptionColumnName = FindHeader(headers, options.DescriptionColumnName, DescriptionHeaders);
        var amountColumns = DetectAmountColumns(headers, options);

        var missing = new List<string>();
        if (dateColumnName is null)
        {
            missing.Add("Date column could not be detected.");
        }

        if (descriptionColumnName is null)
        {
            missing.Add("Description column could not be detected.");
        }

        if (amountColumns is null)
        {
            missing.Add("Amount column or debit/credit columns could not be detected.");
        }

        errors = missing;
        if (dateColumnName is null || descriptionColumnName is null || amountColumns is null)
        {
            layout = null;
            return false;
        }

        layout = new StatementColumnLayout(
            dateColumnName,
            descriptionColumnName,
            FindHeader(headers, options.CategoryColumnName, CategoryHeaders),
            FindHeader(headers, null, CardNumberHeaders),
            KnownStatementIssuers.Detect(headers),
            amountColumns,
            new StatementDateFormat(options.DateFormat));
        return true;
    }

    /// <summary>
    /// Reads one data row. Returns false with a user-facing reason when the row is not a valid transaction.
    /// </summary>
    public bool TryReadRow(
        Func<string, string?> getField,
        int sourceRowNumber,
        [NotNullWhen(true)] out ParsedStatementRow? row,
        [NotNullWhen(false)] out string? error)
    {
        row = null;

        var dateText = getField(DateColumnName);
        var description = getField(DescriptionColumnName);

        if (string.IsNullOrWhiteSpace(dateText) && string.IsNullOrWhiteSpace(description))
        {
            error = "Row is empty.";
            return false;
        }

        if (!DateFormat.TryParse(dateText, out var postedDate))
        {
            error = $"Date '{dateText}' could not be parsed.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            error = "Description is missing.";
            return false;
        }

        if (!AmountColumns.TryRead(getField, out var amount))
        {
            error = "Amount could not be parsed.";
            return false;
        }

        error = null;
        row = new ParsedStatementRow(
            postedDate,
            description.Trim(),
            amount,
            sourceRowNumber,
            ReadOptionalField(getField, CategoryColumnName));
        return true;
    }

    public string? ReadCardNumber(Func<string, string?> getField)
    {
        return ReadOptionalField(getField, CardNumberColumnName);
    }

    private static StatementAmountColumns? DetectAmountColumns(
        IReadOnlyList<string> headers,
        StatementParseOptions options)
    {
        var amountColumnName = FindHeader(headers, options.AmountColumnName, AmountHeaders);
        if (amountColumnName is not null)
        {
            return new SignedAmountColumn(amountColumnName, options.SignedAmountExpensesAreNegative);
        }

        var debitColumnName = FindHeader(headers, options.DebitColumnName, DebitHeaders);
        var creditColumnName = FindHeader(headers, options.CreditColumnName, CreditHeaders);
        return debitColumnName is not null && creditColumnName is not null
            ? new DebitCreditColumns(debitColumnName, creditColumnName)
            : null;
    }

    private static string? ReadOptionalField(Func<string, string?> getField, string? columnName)
    {
        if (columnName is null)
        {
            return null;
        }

        var value = getField(columnName);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? FindHeader(
        IReadOnlyList<string> headers,
        string? explicitHeader,
        IReadOnlyList<string> candidates)
    {
        if (!string.IsNullOrWhiteSpace(explicitHeader))
        {
            return headers.FirstOrDefault(header => StatementHeaderName.Matches(header, explicitHeader));
        }

        foreach (var candidate in candidates)
        {
            var match = headers.FirstOrDefault(header => StatementHeaderName.Matches(header, candidate));
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }
}
