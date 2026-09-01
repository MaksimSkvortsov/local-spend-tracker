namespace Spendnest.Desktop.Presentation.Settings;

public sealed record SettingsTabItem(
    SettingsTab Tab,
    string Icon,
    string Label,
    string Subtitle,
    string Url);
