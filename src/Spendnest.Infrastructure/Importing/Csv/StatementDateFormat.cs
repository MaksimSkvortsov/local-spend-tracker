using System.Globalization;

namespace Spendnest.Infrastructure.Importing.Csv;

/// <summary>
/// Parses statement dates with an explicit format, or with the common bank formats when none is given.
/// </summary>
public sealed class StatementDateFormat
{
    private static readonly string[] CommonFormats =
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

    private readonly string? explicitFormat;

    public StatementDateFormat(string? explicitFormat = null)
    {
        this.explicitFormat = string.IsNullOrWhiteSpace(explicitFormat) ? null : explicitFormat;
    }

    public bool TryParse(string? value, out DateOnly date)
    {
        date = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var text = value.Trim();
        if (explicitFormat is not null)
        {
            return DateOnly.TryParseExact(text, explicitFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        return DateOnly.TryParseExact(text, CommonFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
            || DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}
