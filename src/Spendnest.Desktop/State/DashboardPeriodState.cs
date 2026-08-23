namespace Spendnest.Desktop.State;

public sealed class DashboardPeriodState
{
    public string Mode { get; set; } = "Year";

    public int? Year { get; set; }

    public int? Month { get; set; }
}
