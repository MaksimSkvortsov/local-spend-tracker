namespace Spendnest.UiHarness;

/// <summary>
/// The system clock with a configurable local time zone. Browser tests set <c>UiHarness:TimeZone</c> so dates
/// the app converts from UTC come out the same on every machine; without it the machine's time zone is used.
/// </summary>
public sealed class UiHarnessTimeProvider(TimeZoneInfo localTimeZone) : TimeProvider
{
    public const string TimeZoneKey = "UiHarness:TimeZone";

    public override TimeZoneInfo LocalTimeZone => localTimeZone;

    public static TimeProvider FromConfiguration(IConfiguration configuration)
    {
        var timeZoneId = configuration[TimeZoneKey];

        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return System;
        }

        return TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var timeZone)
            ? new UiHarnessTimeProvider(timeZone)
            : throw new InvalidOperationException($"{TimeZoneKey} is not a known time zone: '{timeZoneId}'.");
    }
}
