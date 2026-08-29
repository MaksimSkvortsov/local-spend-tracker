namespace Spendnest.Desktop.Presentation.Dashboard;

public sealed record DashboardLoadRequest(
    bool PreserveSelectedWindow,
    DateOnly? FocusDate,
    ReportMode CurrentMode,
    int CurrentYear,
    int CurrentMonth,
    string? StoredMode,
    int? StoredYear,
    int? StoredMonth);
