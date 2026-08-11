using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Spendnest.Core.Ai;
using Spendnest.Core.Categorization;
using Spendnest.Core.Credentials;
using Spendnest.Core.Importing;
using Spendnest.Core.Reporting;
using Spendnest.Core.Review;
using Spendnest.App.State;
using Spendnest.Infrastructure.Categorization;
using Spendnest.Infrastructure.Credentials;
using Spendnest.Infrastructure.Importing;
using Spendnest.Infrastructure.Persistence;
using Spendnest.Infrastructure.Reporting;
using Spendnest.Infrastructure.Review;

namespace Spendnest.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        EnvironmentFileLoader.Load(".env.local");

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables("SPENDNEST_")
            .Build();

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSingleton<IConfiguration>(configuration);
        builder.Services.AddSingleton<AppDataRefreshNotifier>();
        builder.Services.AddSingleton<IStatementParser, CsvStatementParser>();
        builder.Services.AddSingleton<ICredentialStore>(_ => new InMemoryCredentialStore(new Dictionary<string, string?>
        {
            [CredentialKeys.OpenAiApiKey] = ReadConfiguredOpenAiApiKey(configuration)
        }));
        builder.Services.AddSpendnestSqlitePersistence();
        builder.Services.AddSingleton<IStatementFileImportService, StatementFileImportService>();
        builder.Services.AddSingleton<ITransactionMerchantCodeResolver, TransactionMerchantCodeResolver>();
        builder.Services.AddSingleton<ILocalTransactionCategorizer, LocalTransactionCategorizer>();
        builder.Services.AddSingleton<HttpClient>();
        builder.Services.AddSingleton(new OpenAiCategorizerOptions
        {
            Model = configuration["OpenAI:Model"] ?? "gpt-5.6-luna"
        });
        builder.Services.AddSingleton<ITransactionCategorizer, StoredOpenAiTransactionCategorizer>();
        builder.Services.AddSingleton<ITransactionCategorizationService, TransactionCategorizationService>();
        builder.Services.AddSingleton<ITransactionCategorizationApplier, TransactionCategorizationApplier>();
        builder.Services.AddSingleton<ICategorySpendingReportService, CategorySpendingReportService>();
        builder.Services.AddSingleton<ITransactionReviewService, TransactionReviewService>();
        builder.Services.AddSingleton<IAiConnectionTestService, OpenAiConnectionTestService>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        app.Services
            .GetRequiredService<SpendnestDatabaseInitializer>()
            .InitializeAsync(CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        return app;
    }

    private static string? ReadConfiguredOpenAiApiKey(IConfiguration configuration)
    {
        return configuration["OpenAI:ApiKey"]
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    }
}
