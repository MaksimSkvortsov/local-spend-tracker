using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spendnest.Core.Importing;
using Spendnest.Core;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Importing;
using Spendnest.Infrastructure;
using Spendnest.Infrastructure.Transactions;

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

if (command.Equals("help", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("Spendnest console");
    Console.WriteLine();
    Console.WriteLine("Available now:");
    Console.WriteLine("  help");
    Console.WriteLine("  parse <csv-file>");
    Console.WriteLine("  import <csv-file>");
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
