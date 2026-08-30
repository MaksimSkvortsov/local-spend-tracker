using Spendnest.Application;
using Spendnest.Application.Importing;
using Spendnest.Core.Ai;
using Spendnest.Core.Categorization;
using Spendnest.Core.Credentials;
using Spendnest.Core.Importing;
using Spendnest.Core.Reporting;
using Spendnest.Core.Review;
using Spendnest.Desktop;
using Spendnest.Desktop.Services;
using Spendnest.Desktop.State;
using Spendnest.Infrastructure.Categorization;
using Spendnest.Infrastructure.Credentials;
using Spendnest.Infrastructure.Importing;
using Spendnest.Infrastructure.Logging;
using Spendnest.Infrastructure.Persistence;
using Spendnest.UiHarness;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables("SPENDNEST_");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<AppDataRefreshNotifier>();
builder.Services.AddSingleton<DashboardPeriodState>();
builder.Services.AddSingleton<DashboardService>();
builder.Services.AddSingleton<ImportFileSelectionService>();
builder.Services.AddSingleton<ImportPageService>();
builder.Services.AddSingleton<ImportWorkflowService>();
builder.Services.AddSingleton<TransactionsPageService>();
builder.Services.AddSingleton<IStatementFilePicker, DevStatementFilePicker>();
builder.Services.AddSingleton<IStatementParser, CsvStatementParser>();
builder.Services.AddSingleton<IStatementFileReader, LocalStatementFileReader>();
builder.Services.AddSingleton<ICredentialStore, InMemoryCredentialStore>();
builder.Services.AddSpendnestSqlitePersistence();
builder.Services.AddSingleton<ITransactionMerchantCodeResolver, TransactionMerchantCodeResolver>();
builder.Services.AddSingleton<LocalCategoryRuleMatcher>();
builder.Services.AddSingleton<ILocalTransactionCategorizer, LocalTransactionCategorizer>();
builder.Services.AddSingleton<HttpClient>();
builder.Services.AddSingleton(new OpenAiCategorizerOptions
{
    Model = builder.Configuration["OpenAI:Model"] ?? "gpt-5.6-luna"
});
builder.Services.AddSingleton<ITransactionCategorizer, BatchedOpenAiTransactionCategorizer>();
builder.Services.AddSpendnestApplicationServices();
builder.Services.AddSingleton<IAiConnectionTestService, OpenAiConnectionTestService>();

builder.Logging
    .AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning)
    .AddSpendnestFile();

var app = builder.Build();

app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.GetFullPath(Path.Combine(
            app.Environment.ContentRootPath,
            "..",
            "Spendnest.Desktop",
            "wwwroot"))),
    RequestPath = string.Empty
});
app.UseAntiforgery();

await app.Services
    .GetRequiredService<SpendnestDatabaseInitializer>()
    .InitializeAsync(CancellationToken.None);

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
