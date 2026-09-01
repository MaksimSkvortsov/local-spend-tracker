namespace Spendnest.Desktop.Presentation.Settings;

public sealed record SettingsOperationResult(
    string Message,
    SettingsStatusKind StatusKind);
