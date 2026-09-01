namespace Spendnest.Desktop.Presentation.Settings;

public sealed record SettingsPageData(
    bool HasConfiguredApiKey,
    string Model,
    int RequestTimeoutSeconds);
