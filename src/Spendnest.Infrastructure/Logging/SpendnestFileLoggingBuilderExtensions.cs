using Microsoft.Extensions.Logging;

namespace Spendnest.Infrastructure.Logging;

public static class SpendnestFileLoggingBuilderExtensions
{
    public static ILoggingBuilder AddSpendnestFile(
        this ILoggingBuilder builder,
        string? logPath = null,
        LogLevel minimumLevel = LogLevel.Information)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddProvider(new SpendnestFileLoggerProvider(logPath, minimumLevel));

        return builder;
    }
}
