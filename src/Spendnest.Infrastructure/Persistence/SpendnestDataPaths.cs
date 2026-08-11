namespace Spendnest.Infrastructure.Persistence;

public static class SpendnestDataPaths
{
    private const string AppDirectoryName = "Spendnest";

    public static string GetDefaultDatabasePath()
    {
        return Path.Combine(GetDefaultAppDataDirectory(), "spendnest.db");
    }

    public static string GetDefaultLogPath()
    {
        var logDirectory = Path.Combine(GetDefaultAppDataDirectory(), "logs");
        Directory.CreateDirectory(logDirectory);

        return Path.Combine(logDirectory, "spendnest.log");
    }

    public static string GetDefaultConnectionString()
    {
        return $"Data Source={GetDefaultDatabasePath()}";
    }

    private static string GetDefaultAppDataDirectory()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var spendnestPath = Path.Combine(appDataPath, AppDirectoryName);

        Directory.CreateDirectory(spendnestPath);

        return spendnestPath;
    }
}
