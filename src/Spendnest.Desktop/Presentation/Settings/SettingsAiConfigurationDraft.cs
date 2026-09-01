namespace Spendnest.Desktop.Presentation.Settings;

public sealed class SettingsAiConfigurationDraft
{
    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public int RequestTimeoutSeconds { get; set; }

    public bool HasConfiguredApiKey { get; set; }

    public bool ShowApiKey { get; set; }

    public static SettingsAiConfigurationDraft From(SettingsPageData pageData)
    {
        return new SettingsAiConfigurationDraft
        {
            HasConfiguredApiKey = pageData.HasConfiguredApiKey,
            Model = pageData.Model,
            RequestTimeoutSeconds = pageData.RequestTimeoutSeconds
        };
    }

    public SettingsAiConfigurationState ToState(
        bool isBusy,
        string? statusMessage,
        SettingsStatusKind statusKind)
    {
        return new SettingsAiConfigurationState(
            ApiKey,
            Model,
            RequestTimeoutSeconds,
            HasConfiguredApiKey,
            ShowApiKey,
            isBusy,
            statusMessage,
            statusKind);
    }

    public SettingsAiConfigurationRequest ToRequest()
    {
        return new SettingsAiConfigurationRequest(
            ApiKey,
            HasConfiguredApiKey,
            Model,
            RequestTimeoutSeconds);
    }
}
