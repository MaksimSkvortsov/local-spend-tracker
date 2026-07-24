namespace Spendnest.Console;

/// <summary>
/// Converts a year-month argument into an inclusive transaction date range.
/// </summary>
public sealed record ReportMonth(
    int Year,
    int Month)
{
    public DateOnly StartDate => new(Year, Month, 1);

    public DateOnly EndDate => StartDate.AddMonths(1).AddDays(-1);

    public static bool TryParse(
        string value,
        out ReportMonth? reportMonth)
    {
        reportMonth = null;
        var parts = value.Split('-', StringSplitOptions.TrimEntries);
        if (parts.Length != 2
            || !int.TryParse(parts[0], out var year)
            || !int.TryParse(parts[1], out var month)
            || month is < 1 or > 12)
        {
            return false;
        }

        reportMonth = new ReportMonth(year, month);
        return true;
    }
}
