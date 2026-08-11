using Microsoft.Extensions.Logging;

namespace Spendnest.Infrastructure.Logging;

internal sealed class SpendnestFileLogger : ILogger
{
    private readonly string categoryName;
    private readonly SpendnestFileLoggerProvider provider;

    public SpendnestFileLogger(
        string categoryName,
        SpendnestFileLoggerProvider provider)
    {
        this.categoryName = categoryName;
        this.provider = provider;
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return provider.IsEnabled(logLevel);
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        if (!IsEnabled(logLevel))
        {
            return;
        }

        provider.Write(
            logLevel,
            categoryName,
            eventId,
            formatter(state, exception),
            exception);
    }
}
