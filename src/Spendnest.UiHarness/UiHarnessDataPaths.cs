namespace Spendnest.UiHarness;

/// <summary>
/// Keeps harness data separate from the desktop app's database and logs, so browser
/// checks and automated UI tests never touch real user data.
/// </summary>
public sealed record UiHarnessDataPaths(string DatabasePath, string LogPath)
{
    public const string DataDirectoryKey = "UiHarness:DataDirectory";

    public string ConnectionString => $"Data Source={DatabasePath}";

    public static UiHarnessDataPaths FromConfiguration(IConfiguration configuration)
    {
        var configuredDirectory = configuration[DataDirectoryKey];

        if (!string.IsNullOrWhiteSpace(configuredDirectory) && !Path.IsPathFullyQualified(configuredDirectory))
        {
            throw new InvalidOperationException(
                $"{DataDirectoryKey} must be an absolute path: '{configuredDirectory}'.");
        }

        var dataDirectory = string.IsNullOrWhiteSpace(configuredDirectory)
            ? GetDefaultDataDirectory()
            : configuredDirectory;

        Directory.CreateDirectory(dataDirectory);

        var logDirectory = Path.Combine(dataDirectory, "logs");
        Directory.CreateDirectory(logDirectory);

        return new UiHarnessDataPaths(
            Path.Combine(dataDirectory, "spendnest.db"),
            Path.Combine(logDirectory, "spendnest.log"));
    }

    private static string GetDefaultDataDirectory()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appDataPath, "Spendnest", "UiHarness");
    }
}
