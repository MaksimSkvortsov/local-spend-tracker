using Microsoft.Extensions.Logging;

namespace Spendnest.Console;

/// <summary>
/// Runs Spendnest console commands either once or in an interactive session.
/// </summary>
public sealed class SpendnestConsoleApp
{
    private readonly SpendnestCommandDispatcher dispatcher;
    private readonly ILogger<SpendnestConsoleApp> logger;

    public SpendnestConsoleApp(
        SpendnestCommandDispatcher dispatcher,
        ILogger<SpendnestConsoleApp> logger)
    {
        this.dispatcher = dispatcher;
        this.logger = logger;
    }

    public async Task<int> RunAsync(
        IReadOnlyList<string> args,
        CancellationToken cancellationToken)
    {
        if (args.Count == 0)
        {
            return await dispatcher.ExecuteAsync(["help"], cancellationToken).ConfigureAwait(false);
        }

        if (args[0].Equals("run", StringComparison.OrdinalIgnoreCase))
        {
            return await RunInteractiveAsync(cancellationToken).ConfigureAwait(false);
        }

        return await dispatcher.ExecuteAsync(args, cancellationToken).ConfigureAwait(false);
    }

    private async Task<int> RunInteractiveAsync(CancellationToken cancellationToken)
    {
        System.Console.WriteLine("Spendnest console");
        System.Console.WriteLine("Type help for commands. Type exit to leave.");
        System.Console.WriteLine();

        while (!cancellationToken.IsCancellationRequested)
        {
            System.Console.Write("spendnest> ");
            var commandLine = System.Console.ReadLine();
            if (commandLine is null)
            {
                return 0;
            }

            var args = CommandLineTokenizer.Tokenize(commandLine);
            if (args.Count == 0)
            {
                continue;
            }

            if (args[0].Equals("exit", StringComparison.OrdinalIgnoreCase)
                || args[0].Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            try
            {
                await dispatcher.ExecuteAsync(args, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Command failed.");
                System.Console.Error.WriteLine(exception.Message);
            }

            System.Console.WriteLine();
        }

        return 0;
    }
}
