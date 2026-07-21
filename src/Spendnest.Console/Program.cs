using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spendnest.Core;
using Spendnest.Infrastructure;

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
    .BuildServiceProvider();

var logger = serviceProvider
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("Spendnest.Console");

var command = args.FirstOrDefault() ?? "help";

if (command.Equals("help", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("Spendnest console");
    Console.WriteLine();
    Console.WriteLine("Available now:");
    Console.WriteLine("  help");
    Console.WriteLine();
    Console.WriteLine("Planned:");
    Console.WriteLine("  init");
    Console.WriteLine("  preview <csv-file>");
    Console.WriteLine("  import <csv-file>");
    Console.WriteLine("  list-transactions");
    Console.WriteLine("  categorize");
    Console.WriteLine("  review");
    Console.WriteLine("  summary <yyyy-mm>");

    return;
}

logger.LogWarning("Command '{Command}' is not implemented yet.", command);
Environment.ExitCode = 1;
