using Microsoft.Extensions.DependencyInjection;
using Spendnest.Application.Importing;
using Spendnest.Core.Importing;
using Spendnest.Infrastructure.Importing.Csv;

namespace Spendnest.Infrastructure.Importing;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers statement file reading and parsing. Hosts depend on <see cref="IStatementParser"/>;
    /// the supported file format is decided here.
    /// </summary>
    public static IServiceCollection AddSpendnestStatementImporting(this IServiceCollection services)
    {
        services.AddSingleton<IStatementParser, CsvStatementParser>();
        services.AddSingleton<IStatementFileReader, LocalStatementFileReader>();

        return services;
    }
}
