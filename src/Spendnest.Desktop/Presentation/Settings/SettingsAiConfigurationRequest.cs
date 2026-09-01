namespace Spendnest.Desktop.Presentation.Settings;

public sealed record SettingsAiConfigurationRequest(
    string ApiKey,
    bool HasConfiguredApiKey,
    string Model,
    int RequestTimeoutSeconds);
