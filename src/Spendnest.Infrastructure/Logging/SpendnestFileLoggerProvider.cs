using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Spendnest.Infrastructure.Persistence;

namespace Spendnest.Infrastructure.Logging;

public sealed class SpendnestFileLoggerProvider : ILoggerProvider
{
    private const long MaxLogBytes = 1_000_000;

    private readonly ConcurrentDictionary<string, SpendnestFileLogger> loggers = new(StringComparer.Ordinal);
    private readonly object writeLock = new();
    private readonly string logPath;
    private readonly LogLevel minimumLevel;

    public SpendnestFileLoggerProvider(
        string? logPath = null,
        LogLevel minimumLevel = LogLevel.Information)
    {
        this.logPath = string.IsNullOrWhiteSpace(logPath)
            ? SpendnestDataPaths.GetDefaultLogPath()
            : logPath;
        this.minimumLevel = minimumLevel;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return loggers.GetOrAdd(categoryName, name => new SpendnestFileLogger(name, this));
    }

    public void Dispose()
    {
        loggers.Clear();
    }

    internal bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None && logLevel >= minimumLevel;
    }

    internal void Write(
        LogLevel logLevel,
        string categoryName,
        EventId eventId,
        string message,
        Exception? exception)
    {
        if (string.IsNullOrWhiteSpace(message) && exception is null)
        {
            return;
        }

        lock (writeLock)
        {
            var logDirectory = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrWhiteSpace(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            RotateIfNeeded();

            using var stream = new FileStream(
                logPath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.ReadWrite);
            using var writer = new StreamWriter(stream);

            writer.Write(DateTimeOffset.Now.ToString("O"));
            writer.Write(" [");
            writer.Write(logLevel);
            writer.Write("] ");
            writer.Write(categoryName);
            if (eventId.Id != 0)
            {
                writer.Write(" (");
                writer.Write(eventId.Id);
                writer.Write(')');
            }

            writer.Write(": ");
            writer.WriteLine(message);

            if (exception is not null)
            {
                writer.WriteLine(exception);
            }
        }
    }

    private void RotateIfNeeded()
    {
        var file = new FileInfo(logPath);
        if (!file.Exists || file.Length < MaxLogBytes)
        {
            return;
        }

        var archivePath = Path.ChangeExtension(logPath, ".1.log");
        File.Delete(archivePath);
        File.Move(logPath, archivePath);
    }
}
