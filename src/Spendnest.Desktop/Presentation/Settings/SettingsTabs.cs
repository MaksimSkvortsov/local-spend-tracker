namespace Spendnest.Desktop.Presentation.Settings;

public static class SettingsTabs
{
    public static readonly SettingsTabItem Ai = new(
        SettingsTab.Ai,
        "psychology",
        "AI Configuration",
        "Configure your language model provider for smart transaction categorization.",
        "settings");

    public static readonly SettingsTabItem Data = new(
        SettingsTab.Data,
        "database",
        "Data & Privacy",
        "Manage your local database, backups, and privacy controls.",
        "settings?tab=data");

    public static readonly SettingsTabItem DevTesting = new(
        SettingsTab.DevTesting,
        "bug_report",
        "Developer Testing",
        "Locate diagnostic files used while testing packaged and local builds.",
        "settings?tab=dev-testing");

    public static IReadOnlyList<SettingsTabItem> Items { get; } =
    [
        Ai,
        Data,
        DevTesting
    ];

    public static SettingsTabItem Get(SettingsTab tab)
    {
        return Items.First(item => item.Tab == tab);
    }

    public static SettingsTab FromQueryValue(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "data" => SettingsTab.Data,
            "dev-testing" => SettingsTab.DevTesting,
            _ => SettingsTab.Ai
        };
    }
}
