namespace Spendnest.Infrastructure.Persistence;

public static class SpendnestDataPaths
{
    public static string GetDefaultDatabasePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var spendnestPath = Path.Combine(appDataPath, "Spendnest");

        Directory.CreateDirectory(spendnestPath);

        return Path.Combine(spendnestPath, "spendnest.db");
    }

    public static string GetDefaultConnectionString()
    {
        return $"Data Source={GetDefaultDatabasePath()}";
    }
}
