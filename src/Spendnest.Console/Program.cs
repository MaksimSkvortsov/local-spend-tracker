using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spendnest.Console;
using Spendnest.Core;
using Spendnest.Core.Accounts;
using Spendnest.Core.Categorization;
using Spendnest.Core.Credentials;
using Spendnest.Core.Importing;
using Spendnest.Core.Reporting;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure;
using Spendnest.Infrastructure.Accounts;
using Spendnest.Infrastructure.Categorization;
using Spendnest.Infrastructure.Credentials;
using Spendnest.Infrastructure.Importing;
using Spendnest.Infrastructure.Reporting;
using Spendnest.Infrastructure.Transactions;

ConsoleEnvironment.LoadLocalEnvironmentFile(".env.local");

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables("SPENDNEST_")
    .Build();

using var serviceProvider = new ServiceCollection()
    .AddSingleton<IConfiguration>(configuration)
    .AddLogging(builder => builder.AddSimpleConsole())
    .AddSingleton<CoreAssemblyMarker>()
    .AddSingleton<InfrastructureAssemblyMarker>()
    .AddSingleton<IStatementParser, CsvStatementParser>()
    .AddSingleton<ICredentialStore>(_ => new InMemoryCredentialStore(new Dictionary<string, string?>
    {
        [CredentialKeys.OpenAiApiKey] = ReadConfiguredOpenAiApiKey(configuration)
    }))
    .AddSingleton<ICardAccountRepository, InMemoryCardAccountRepository>()
    .AddSingleton<ITransactionRepository, InMemoryTransactionRepository>()
    .AddSingleton<IStatementFileImportService, StatementFileImportService>()
    .AddSingleton<ITransactionCategoryMapper, KeywordTransactionCategoryMapper>()
    .AddSingleton<ICategoryRuleRepository, InMemoryCategoryRuleRepository>()
    .AddSingleton<ILocalTransactionCategorizer, LocalTransactionCategorizer>()
    .AddSingleton<FakeTransactionCategorizer>()
    .AddSingleton<HttpClient>()
    .AddSingleton(new OpenAiCategorizerOptions
    {
        Model = configuration["OpenAI:Model"] ?? "gpt-5.6-sol"
    })
    .AddSingleton<ITransactionCategorizer, StoredOpenAiTransactionCategorizer>()
    .AddSingleton<ITransactionCategorizationService, TransactionCategorizationService>()
    .AddSingleton<ICategorySpendingReportService, CategorySpendingReportService>()
    .AddSingleton<SpendnestCommandDispatcher>()
    .AddSingleton<SpendnestConsoleApp>()
    .BuildServiceProvider();

var app = serviceProvider.GetRequiredService<SpendnestConsoleApp>();
Environment.ExitCode = await app.RunAsync(args, CancellationToken.None);

static string? ReadConfiguredOpenAiApiKey(IConfiguration configuration)
{
    return configuration["OpenAI:ApiKey"]
        ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
}
