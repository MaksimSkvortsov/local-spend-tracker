using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spendnest.Application;
using Spendnest.Application.Importing;
using Spendnest.Console;
using Spendnest.Core;
using Spendnest.Core.Categorization;
using Spendnest.Core.Credentials;
using Spendnest.Core.Importing;
using Spendnest.Core.Reporting;
using Spendnest.Core.Review;
using Spendnest.Infrastructure;
using Spendnest.Infrastructure.Categorization;
using Spendnest.Infrastructure.Credentials;
using Spendnest.Infrastructure.Importing;
using Spendnest.Infrastructure.Logging;
using Spendnest.Infrastructure.Persistence;

ConsoleEnvironment.LoadLocalEnvironmentFile(".env.local");

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables("SPENDNEST_")
    .Build();

using var serviceProvider = new ServiceCollection()
    .AddSingleton<IConfiguration>(configuration)
    .AddLogging(builder => builder
        .AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning)
        .AddSpendnestFile()
        .AddSimpleConsole())
    .AddSingleton<CoreAssemblyMarker>()
    .AddSingleton<InfrastructureAssemblyMarker>()
    .AddSingleton<IStatementParser, CsvStatementParser>()
    .AddSingleton<IStatementFileReader, LocalStatementFileReader>()
    .AddSingleton<ICredentialStore>(_ => new InMemoryCredentialStore(new Dictionary<string, string?>
    {
        [CredentialKeys.OpenAiApiKey] = ReadConfiguredOpenAiApiKey(configuration)
    }))
    .AddSpendnestSqlitePersistence()
    .AddSingleton<ITransactionMerchantCodeResolver, TransactionMerchantCodeResolver>()
    .AddSingleton<ILocalTransactionCategorizer, LocalTransactionCategorizer>()
    .AddSingleton<HttpClient>()
    .AddSingleton(new OpenAiCategorizerOptions
    {
        Model = configuration["OpenAI:Model"] ?? "gpt-5.6-luna"
    })
    .AddSingleton<ITransactionCategorizer, StoredOpenAiTransactionCategorizer>()
    .AddSpendnestApplicationServices()
    .AddSingleton<SpendnestCommandDispatcher>()
    .AddSingleton<SpendnestConsoleApp>()
    .BuildServiceProvider();

await serviceProvider
    .GetRequiredService<SpendnestDatabaseInitializer>()
    .InitializeAsync(CancellationToken.None)
    .ConfigureAwait(false);

var app = serviceProvider.GetRequiredService<SpendnestConsoleApp>();
Environment.ExitCode = await app.RunAsync(args, CancellationToken.None);

static string? ReadConfiguredOpenAiApiKey(IConfiguration configuration)
{
    return configuration["OpenAI:ApiKey"]
        ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
}
