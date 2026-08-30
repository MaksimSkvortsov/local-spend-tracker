using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Spendnest.Desktop.Infrastructure;
using Spendnest.Desktop.Infrastructure.Credentials;
using Spendnest.Application;
using Spendnest.Application.Importing;
using Spendnest.Core.Ai;
using Spendnest.Core.Categorization;
using Spendnest.Core.Credentials;
using Spendnest.Core.Importing;
using Spendnest.Core.Reporting;
using Spendnest.Core.Review;
using Spendnest.Desktop.Services;
using Spendnest.Desktop.State;
using Spendnest.Infrastructure.Categorization;
using Spendnest.Infrastructure.Importing;
using Spendnest.Infrastructure.Logging;
using Spendnest.Infrastructure.Persistence;

namespace Spendnest.Desktop;

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
        builder.Services.AddSingleton<DashboardPeriodState>();
        builder.Services.AddSingleton<DashboardService>();
        builder.Services.AddSingleton<ImportFileSelectionService>();
        builder.Services.AddSingleton<ImportPageService>();
        builder.Services.AddSingleton<ImportWorkflowService>();
        builder.Services.AddSingleton<IStatementFilePicker, MauiStatementFilePicker>();
        builder.Services.AddSingleton<IStatementParser, CsvStatementParser>();
        builder.Services.AddSingleton<IStatementFileReader, LocalStatementFileReader>();
        builder.Services.AddSingleton<ICredentialStore, SecureStorageCredentialStore>();
        builder.Services.AddSpendnestSqlitePersistence();
        builder.Services.AddSingleton<ITransactionMerchantCodeResolver, TransactionMerchantCodeResolver>();
        builder.Services.AddSingleton<LocalCategoryRuleMatcher>();
        builder.Services.AddSingleton<ILocalTransactionCategorizer, LocalTransactionCategorizer>();
        builder.Services.AddSingleton<HttpClient>();
        builder.Services.AddSingleton(new OpenAiCategorizerOptions
        {
            Model = configuration["OpenAI:Model"] ?? "gpt-5.6-luna"
        });
        builder.Services.AddSingleton<ITransactionCategorizer, BatchedOpenAiTransactionCategorizer>();
        builder.Services.AddSpendnestApplicationServices();
        builder.Services.AddSingleton<IAiConnectionTestService, OpenAiConnectionTestService>();

        builder.Logging
            .AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning)
            .AddSpendnestFile();

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
}
