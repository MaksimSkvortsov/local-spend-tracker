using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Spendnest.Core.Importing;

namespace Spendnest.Infrastructure.Importing.Csv;

/// <summary>
/// Parses comma-separated credit-card statement files using CsvHelper.
/// Column detection and row rules live in <see cref="StatementColumnLayout"/>.
/// </summary>
public sealed class CsvStatementParser : IStatementParser
{
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
            const string emptyFileError = "CSV file is empty.";
            return new StatementParseResult([], [new StatementParseWarning(null, emptyFileError)], 0, 0, emptyFileError);
        }

        var header = await FindTransactionHeaderAsync(csv, options).ConfigureAwait(false);
        if (header.Layout is null)
        {
            var columnWarnings = header.FirstRowErrors
                .Select(columnError => new StatementParseWarning(null, columnError))
                .ToArray();
            return new StatementParseResult([], columnWarnings, 0, 0, columnWarnings[0].Message);
        }

        var layout = header.Layout;
        string? GetField(string columnName) => csv.GetField(columnName);

        var rows = new List<ParsedStatementRow>();
        var warnings = new List<StatementParseWarning>();
        var totalRows = 0;
        string? cardNumber = null;

        while (await csv.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            totalRows++;
            var sourceRowNumber = csv.Parser.Row;

            if (totalRows == 1)
            {
                cardNumber = layout.ReadCardNumber(GetField);
            }

            if (IsPastPreviewLimit(options, rows.Count))
            {
                continue;
            }

            if (layout.TryReadRow(GetField, sourceRowNumber, out var row, out var rowError))
            {
                rows.Add(row);
            }
            else
            {
                warnings.Add(new StatementParseWarning(sourceRowNumber, rowError));
            }
        }

        var error = rows.Count == 0 ? warnings.FirstOrDefault()?.Message : null;
        return new StatementParseResult(rows, warnings, totalRows, warnings.Count, error, cardNumber, layout.CardIssuer);
    }

    /// <summary>
    /// Finds the first row that is a valid transaction header, starting at the current record, so summary
    /// tables above the transactions are skipped. When no row qualifies, returns the first row's column errors,
    /// which is what a single-table file with a bad header reports.
    /// </summary>
    private static async Task<TransactionHeader> FindTransactionHeaderAsync(
        CsvReader csv,
        StatementParseOptions options)
    {
        IReadOnlyList<string>? firstRowErrors = null;

        do
        {
            var candidate = csv.Parser.Record ?? [];
            if (StatementColumnLayout.TryDetect(candidate, options, out var layout, out var errors))
            {
                csv.ReadHeader();
                return new TransactionHeader(layout, []);
            }

            firstRowErrors ??= errors;
        }
        while (await csv.ReadAsync().ConfigureAwait(false));

        return new TransactionHeader(null, firstRowErrors);
    }

    private static bool IsPastPreviewLimit(StatementParseOptions options, int parsedRowCount)
    {
        return options.PreviewOnly
            && options.PreviewRowLimit is not null
            && parsedRowCount >= options.PreviewRowLimit.Value;
    }

    private sealed record TransactionHeader(
        StatementColumnLayout? Layout,
        IReadOnlyList<string> FirstRowErrors);
}
