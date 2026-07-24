using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spendnest.Core.Importing;
using Spendnest.Core;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Reporting;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Categorization;
using Spendnest.Infrastructure.Importing;
using Spendnest.Infrastructure.Reporting;
using Spendnest.Infrastructure;
using Spendnest.Infrastructure.Transactions;

LoadLocalEnvironmentFile(".env.local");

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
    .AddSingleton<ITransactionRepository, InMemoryTransactionRepository>()
    .AddSingleton<IStatementFileImportService, StatementFileImportService>()
    .AddSingleton<ITransactionCategoryMapper, KeywordTransactionCategoryMapper>()
    .AddSingleton<ICategoryRuleRepository, InMemoryCategoryRuleRepository>()
    .AddSingleton<ILocalTransactionCategorizer, LocalTransactionCategorizer>()
    .AddSingleton<FakeTransactionCategorizer>()
    .AddSingleton<ITransactionCategorizer>(provider => CreateAiTransactionCategorizer(configuration, provider))
    .AddSingleton<ITransactionCategorizationService, TransactionCategorizationService>()
    .AddSingleton<ICategorySpendingReportService, CategorySpendingReportService>()
    .BuildServiceProvider();

var logger = serviceProvider
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("Spendnest.Console");

var command = args.FirstOrDefault() ?? "help";

if (command.Equals("parse", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Usage: parse <csv-file>");
        Environment.ExitCode = 1;
        return;
    }

    var csvFilePath = args[1];
    if (!File.Exists(csvFilePath))
    {
        Console.Error.WriteLine($"File not found: {csvFilePath}");
        Environment.ExitCode = 1;
        return;
    }

    await using var stream = File.OpenRead(csvFilePath);
    var parser = serviceProvider.GetRequiredService<IStatementParser>();
    var result = await parser.ParseAsync(stream, new StatementParseOptions(), CancellationToken.None);

    Console.WriteLine($"Rows parsed: {result.Rows.Count}");
    Console.WriteLine($"Total rows: {result.TotalRowCount}");
    Console.WriteLine($"Failed rows: {result.FailedRowCount}");
    Console.WriteLine();

    foreach (var row in result.Rows.Take(10))
    {
        Console.WriteLine($"{row.PostedDate:yyyy-MM-dd} | {row.Amount,10:0.00} | {row.OriginalDescription}");
    }

    if (result.Warnings.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("Warnings:");
        foreach (var warning in result.Warnings)
        {
            var prefix = warning.SourceRowNumber is null ? "file" : $"row {warning.SourceRowNumber}";
            Console.WriteLine($"  {prefix}: {warning.Message}");
        }
    }

    return;
}

if (command.Equals("import", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Usage: import <csv-file>");
        Environment.ExitCode = 1;
        return;
    }

    var csvFilePath = args[1];
    if (!File.Exists(csvFilePath))
    {
        Console.Error.WriteLine($"File not found: {csvFilePath}");
        Environment.ExitCode = 1;
        return;
    }

    var importService = serviceProvider.GetRequiredService<IStatementFileImportService>();
    var result = await importService.ImportAsync(csvFilePath, CancellationToken.None);

    Console.WriteLine($"Rows parsed: {result.ParsedRowCount}");
    Console.WriteLine($"Transactions saved: {result.SavedTransactionCount}");
    Console.WriteLine($"Duplicate transactions skipped: {result.SkippedDuplicateTransactionCount}");
    Console.WriteLine($"Failed rows: {result.FailedRowCount}");
    Console.WriteLine();

    foreach (var transaction in result.SavedTransactions.Take(10))
    {
        Console.WriteLine($"{transaction.PostedDate:yyyy-MM-dd} | {transaction.Amount,10:0.00} | {transaction.OriginalDescription}");
    }

    if (result.Warnings.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("Warnings:");
        foreach (var warning in result.Warnings)
        {
            var prefix = warning.SourceRowNumber is null ? "file" : $"row {warning.SourceRowNumber}";
            Console.WriteLine($"  {prefix}: {warning.Message}");
        }
    }

    return;
}

if (command.Equals("report", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Usage: report <csv-file> [csv-file...]");
        Environment.ExitCode = 1;
        return;
    }

    var importService = serviceProvider.GetRequiredService<IStatementFileImportService>();

    foreach (var csvFilePath in args.Skip(1))
    {
        if (!File.Exists(csvFilePath))
        {
            Console.Error.WriteLine($"File not found: {csvFilePath}");
            Environment.ExitCode = 1;
            return;
        }

        await importService.ImportAsync(csvFilePath, CancellationToken.None);
    }

    var reportService = serviceProvider.GetRequiredService<ICategorySpendingReportService>();
    var report = await reportService.BuildAsync(CancellationToken.None);

    Console.WriteLine("Spending by category");
    Console.WriteLine();

    foreach (var line in report.Lines)
    {
        Console.WriteLine($"{line.CategoryName,-24} {line.TransactionCount,4} {line.Amount,12:0.00}");
    }

    Console.WriteLine();
    Console.WriteLine($"Total{"",-23} {report.TotalSpending,12:0.00}");

    return;
}

if (command.Equals("ai-report", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Usage: ai-report <csv-file> [csv-file...]");
        Environment.ExitCode = 1;
        return;
    }

    var importService = serviceProvider.GetRequiredService<IStatementFileImportService>();

    foreach (var csvFilePath in args.Skip(1))
    {
        if (!File.Exists(csvFilePath))
        {
            Console.Error.WriteLine($"File not found: {csvFilePath}");
            Environment.ExitCode = 1;
            return;
        }

        await importService.ImportAsync(csvFilePath, CancellationToken.None);
    }

    var repository = serviceProvider.GetRequiredService<ITransactionRepository>();
    var transactions = await repository.ListAsync(CancellationToken.None);
    var categorizationService = serviceProvider.GetRequiredService<ITransactionCategorizationService>();
    var categorizations = await categorizationService.CategorizeAsync(transactions, CancellationToken.None);
    var categoriesByCode = BuiltInCategories.All.ToDictionary(category => category.Code, category => category.Name);

    Console.WriteLine("Categorization report");
    Console.WriteLine();

    foreach (var categorization in categorizations.OrderBy(item => item.NeedsReview).ThenBy(item => item.CategoryCode))
    {
        var transaction = transactions.Single(item => item.Id == categorization.TransactionId);
        var categoryName = categoriesByCode.GetValueOrDefault(categorization.CategoryCode, categorization.CategoryCode);
        var review = categorization.NeedsReview ? "review" : "ok";

        Console.WriteLine(
            $"{transaction.PostedDate:yyyy-MM-dd} | {transaction.Amount,10:0.00} | {categoryName,-24} | {categorization.Source,-10} | {categorization.Confidence,4:0.00} | {review} | {transaction.OriginalDescription}");
    }

    return;
}

if (command.Equals("help", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("Spendnest console");
    Console.WriteLine();
    Console.WriteLine("Available now:");
    Console.WriteLine("  help");
    Console.WriteLine("  parse <csv-file>");
    Console.WriteLine("  import <csv-file>");
    Console.WriteLine("  report <csv-file> [csv-file...]");
    Console.WriteLine("  ai-report <csv-file> [csv-file...]");
    Console.WriteLine();
    Console.WriteLine("Planned:");
    Console.WriteLine("  init");
    Console.WriteLine("  preview <csv-file>");
    Console.WriteLine("  list-transactions");
    Console.WriteLine("  categorize");
    Console.WriteLine("  review");
    Console.WriteLine("  summary <yyyy-mm>");

    return;
}

logger.LogWarning("Command '{Command}' is not implemented yet.", command);
Environment.ExitCode = 1;

static ITransactionCategorizer CreateAiTransactionCategorizer(
    IConfiguration configuration,
    IServiceProvider serviceProvider)
{
    var apiKey = configuration["OpenAI:ApiKey"]
        ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        return serviceProvider.GetRequiredService<FakeTransactionCategorizer>();
    }

    return new OpenAiTransactionCategorizer(
        new HttpClient(),
        new OpenAiCategorizerOptions
        {
            ApiKey = apiKey,
            Model = configuration["OpenAI:Model"] ?? "gpt-5.6-sol"
        });
}

static void LoadLocalEnvironmentFile(string filePath)
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
