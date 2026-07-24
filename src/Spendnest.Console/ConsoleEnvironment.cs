namespace Spendnest.Console;

/// <summary>
/// Loads local console-only environment settings before app configuration is built.
/// </summary>
public static class ConsoleEnvironment
{
    public static void LoadLocalEnvironmentFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        foreach (var line in File.ReadLines(filePath))
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.Length == 0 || trimmedLine.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = trimmedLine.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = trimmedLine[..separatorIndex].Trim();
            var value = trimmedLine[(separatorIndex + 1)..].Trim().Trim('"');
            if (Environment.GetEnvironmentVariable(key) is null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
