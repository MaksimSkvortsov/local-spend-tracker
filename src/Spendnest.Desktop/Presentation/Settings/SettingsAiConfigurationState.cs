namespace Spendnest.Desktop.Presentation.Settings;

public sealed record SettingsAiConfigurationState(
    string ApiKey,
    string AiModel,
    int RequestTimeoutSeconds,
    bool HasConfiguredApiKey,
    bool ShowApiKey,
    bool IsBusy,
    string? StatusMessage,
    SettingsStatusKind StatusKind)
{
    public bool CanDeleteConfiguration =>
        HasConfiguredApiKey || !string.IsNullOrWhiteSpace(ApiKey);

    public string ApiKeyInputType => ShowApiKey ? "text" : "password";

    public string ApiKeyVisibilityIcon => ShowApiKey ? "visibility_off" : "visibility";

    public string ApiKeyVisibilityLabel => ShowApiKey ? "Hide API key" : "Show API key";

    public string ApiKeyPlaceholder => HasConfiguredApiKey ? "Saved key is configured" : "sk-...";

    public string ApiKeyHelpText => HasConfiguredApiKey
        ? "Leave blank to keep the saved key, or enter a new one to replace it."
        : "The key is stored in platform secure storage on this device.";

    public static SettingsAiConfigurationState Empty { get; } = new(
        string.Empty,
        string.Empty,
        0,
        false,
        false,
        false,
        null,
        default);
}
