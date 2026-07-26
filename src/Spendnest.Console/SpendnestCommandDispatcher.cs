using Microsoft.Extensions.Logging;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Credentials;
using Spendnest.Core.Importing;
using Spendnest.Core.Reporting;
using Spendnest.Core.Review;
using Spendnest.Core.Transactions;

namespace Spendnest.Console;

/// <summary>
/// Dispatches Spendnest console commands to application services.
/// </summary>
public sealed class SpendnestCommandDispatcher
{
    private readonly IStatementParser parser;
    private readonly IStatementFileImportService importService;
    private readonly ITransactionRepository transactionRepository;
    private readonly ICategorySpendingReportService reportService;
    private readonly ITransactionCategorizationService categorizationService;
    private readonly ITransactionCategorizationApplier categorizationApplier;
    private readonly ITransactionReviewService reviewService;
    private readonly ICredentialStore credentialStore;
    private readonly ILogger<SpendnestCommandDispatcher> logger;

    public SpendnestCommandDispatcher(
        IStatementParser parser,
        IStatementFileImportService importService,
        ITransactionRepository transactionRepository,
        ICategorySpendingReportService reportService,
        ITransactionCategorizationService categorizationService,
        ITransactionCategorizationApplier categorizationApplier,
        ITransactionReviewService reviewService,
        ICredentialStore credentialStore,
        ILogger<SpendnestCommandDispatcher> logger)
    {
        this.parser = parser;
        this.importService = importService;
        this.transactionRepository = transactionRepository;
        this.reportService = reportService;
        this.categorizationService = categorizationService;
        this.categorizationApplier = categorizationApplier;
        this.reviewService = reviewService;
        this.credentialStore = credentialStore;
        this.logger = logger;
    }

    public async Task<int> ExecuteAsync(
        IReadOnlyList<string> args,
        CancellationToken cancellationToken)
    {
        var command = args.FirstOrDefault() ?? "help";

        if (command.Equals("parse", StringComparison.OrdinalIgnoreCase))
        {
            return await ParseAsync(args, cancellationToken).ConfigureAwait(false);
        }

        if (command.Equals("import", StringComparison.OrdinalIgnoreCase))
        {
            return await ImportAsync(args, cancellationToken).ConfigureAwait(false);
        }

        if (command.Equals("report", StringComparison.OrdinalIgnoreCase))
        {
            return await ReportAsync(args, cancellationToken).ConfigureAwait(false);
        }

        if (command.Equals("ai-report", StringComparison.OrdinalIgnoreCase)
            || command.Equals("categorize", StringComparison.OrdinalIgnoreCase))
        {
            return await CategorizationReportAsync(args, cancellationToken).ConfigureAwait(false);
        }

        if (command.Equals("ai-key", StringComparison.OrdinalIgnoreCase))
        {
            return await AiKeyAsync(args, cancellationToken).ConfigureAwait(false);
        }

        if (command.Equals("review", StringComparison.OrdinalIgnoreCase))
        {
            return await ReviewAsync(args, cancellationToken).ConfigureAwait(false);
        }

        if (command.Equals("help", StringComparison.OrdinalIgnoreCase))
        {
            PrintHelp();
            return 0;
        }

        logger.LogWarning("Command '{Command}' is not implemented yet.", command);
        System.Console.Error.WriteLine($"Unknown command: {command}");
        return 1;
    }

    private async Task<int> ParseAsync(
        IReadOnlyList<string> args,
        CancellationToken cancellationToken)
    {
        if (args.Count < 2)
        {
            System.Console.Error.WriteLine("Usage: parse <csv-file>");
            return 1;
        }

        var csvFilePath = args[1];
        if (!File.Exists(csvFilePath))
        {
            System.Console.Error.WriteLine($"File not found: {csvFilePath}");
            return 1;
        }

        await using var stream = File.OpenRead(csvFilePath);
        var result = await parser.ParseAsync(stream, new StatementParseOptions(), cancellationToken).ConfigureAwait(false);

        System.Console.WriteLine($"Rows parsed: {result.Rows.Count}");
        System.Console.WriteLine($"Total rows: {result.TotalRowCount}");
        System.Console.WriteLine($"Failed rows: {result.FailedRowCount}");
        System.Console.WriteLine();

        foreach (var row in result.Rows.Take(10))
        {
            System.Console.WriteLine($"{row.PostedDate:yyyy-MM-dd} | {row.Amount,10:0.00} | {row.OriginalDescription}");
        }

        PrintWarnings(result.Warnings);

        return 0;
    }

    private async Task<int> ImportAsync(
        IReadOnlyList<string> args,
        CancellationToken cancellationToken)
    {
        if (args.Count < 2)
        {
            System.Console.Error.WriteLine("Usage: import <csv-file> [--card <card-name>]");
            return 1;
        }

        var csvFilePath = args[1];
        if (!File.Exists(csvFilePath))
        {
            System.Console.Error.WriteLine($"File not found: {csvFilePath}");
            return 1;
        }

        var result = await importService.ImportAsync(
            csvFilePath,
            ParseImportOptions(args.Skip(2)),
            cancellationToken).ConfigureAwait(false);

        System.Console.WriteLine($"Card: {result.CardAccountName}");
        System.Console.WriteLine($"Rows parsed: {result.ParsedRowCount}");
        System.Console.WriteLine($"Transactions saved: {result.SavedTransactionCount}");
        System.Console.WriteLine($"Duplicate transactions skipped: {result.SkippedDuplicateTransactionCount}");
        System.Console.WriteLine($"Failed rows: {result.FailedRowCount}");
        System.Console.WriteLine();

        foreach (var transaction in result.SavedTransactions.Take(10))
        {
            System.Console.WriteLine($"{transaction.PostedDate:yyyy-MM-dd} | {transaction.Amount,10:0.00} | {transaction.OriginalDescription}");
        }

        PrintWarnings(result.Warnings);

        return 0;
    }

    private async Task<int> ReportAsync(
        IReadOnlyList<string> args,
        CancellationToken cancellationToken)
    {
        var query = new TransactionQuery();

        if (args.Count >= 3 && args[1].Equals("month", StringComparison.OrdinalIgnoreCase))
        {
            if (!ReportMonth.TryParse(args[2], out var reportMonth))
            {
                System.Console.Error.WriteLine("Usage: report month <yyyy-mm>");
                return 1;
            }

            query = new TransactionQuery
            {
                StartDate = reportMonth!.StartDate,
                EndDate = reportMonth.EndDate
            };
        }
        else if (args.Count > 1)
        {
            foreach (var csvFilePath in args.Skip(1))
            {
                if (!File.Exists(csvFilePath))
                {
                    System.Console.Error.WriteLine($"File not found: {csvFilePath}");
                    return 1;
                }

                await importService.ImportAsync(csvFilePath, new StatementFileImportOptions(), cancellationToken).ConfigureAwait(false);
            }
        }

        var report = await reportService.BuildAsync(query, cancellationToken).ConfigureAwait(false);
        PrintCategoryReport(report, query);

        return 0;
    }

    private async Task<int> CategorizationReportAsync(
        IReadOnlyList<string> args,
        CancellationToken cancellationToken)
    {
        foreach (var csvFilePath in args.Skip(1))
        {
            if (!File.Exists(csvFilePath))
            {
                System.Console.Error.WriteLine($"File not found: {csvFilePath}");
                return 1;
            }

            await importService.ImportAsync(csvFilePath, new StatementFileImportOptions(), cancellationToken).ConfigureAwait(false);
        }

        var transactions = await transactionRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        var categorizations = await categorizationService.CategorizeAsync(transactions, cancellationToken).ConfigureAwait(false);
        await categorizationApplier.ApplyAsync(categorizations, cancellationToken).ConfigureAwait(false);
        var categoriesById = BuiltInCategories.All.ToDictionary(category => category.Id, category => category.Name);

        System.Console.WriteLine("Categorization report");
        System.Console.WriteLine();

        foreach (var categorization in categorizations.OrderBy(item => item.NeedsReview).ThenBy(item => item.CategoryId))
        {
            var transaction = transactions.Single(item => item.Id == categorization.TransactionId);
            var categoryName = categoriesById.GetValueOrDefault(categorization.CategoryId, categorization.CategoryId.ToString());
            var review = categorization.NeedsReview ? "review" : "ok";

            System.Console.WriteLine(
                $"{transaction.PostedDate:yyyy-MM-dd} | {transaction.Amount,10:0.00} | {categoryName,-24} | {categorization.Source,-10} | {categorization.Confidence,4:0.00} | {review} | {transaction.OriginalDescription}");
        }

        return 0;
    }

    private async Task<int> ReviewAsync(
        IReadOnlyList<string> args,
        CancellationToken cancellationToken)
    {
        if (args.Count < 2 || args[1].Equals("list", StringComparison.OrdinalIgnoreCase))
        {
            var items = await reviewService.ListNeedsReviewAsync(cancellationToken).ConfigureAwait(false);
            if (items.Count == 0)
            {
                System.Console.WriteLine("No transactions need review.");
                return 0;
            }

            System.Console.WriteLine("Transactions needing review");
            System.Console.WriteLine();

            foreach (var item in items)
            {
                System.Console.WriteLine(
                    $"{item.TransactionId} | {item.PostedDate:yyyy-MM-dd} | {item.Amount,10:0.00} | {FormatCategory(item.CategoryId),-24} | {item.Source?.ToString() ?? "Unknown",-10} | {item.Confidence?.ToString("0.00") ?? "--"} | {item.Description}");
            }

            return 0;
        }

        if (args[1].Equals("set", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Count < 4
                || !Guid.TryParse(args[2], out var transactionId)
                || !int.TryParse(args[3], out var categoryId))
            {
                System.Console.Error.WriteLine("Usage: review set <transaction-id> <category-id> [--remember]");
                return 1;
            }

            await reviewService.SetCategoryAsync(
                transactionId,
                categoryId,
                HasFlag(args, "--remember"),
                cancellationToken).ConfigureAwait(false);
            System.Console.WriteLine("Transaction category updated.");
            return 0;
        }

        if (args[1].Equals("confirm", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Count < 3 || !Guid.TryParse(args[2], out var transactionId))
            {
                System.Console.Error.WriteLine("Usage: review confirm <transaction-id> [--remember]");
                return 1;
            }

            await reviewService.ConfirmAsync(
                transactionId,
                HasFlag(args, "--remember"),
                cancellationToken).ConfigureAwait(false);
            System.Console.WriteLine("Transaction category confirmed.");
            return 0;
        }

        System.Console.Error.WriteLine("Usage: review list|set|confirm");
        return 1;
    }

    private async Task<int> AiKeyAsync(
        IReadOnlyList<string> args,
        CancellationToken cancellationToken)
    {
        if (args.Count < 2
            || args[1].Equals("status", StringComparison.OrdinalIgnoreCase))
        {
            var apiKey = await credentialStore.GetStringAsync(CredentialKeys.OpenAiApiKey, cancellationToken).ConfigureAwait(false);
            System.Console.WriteLine(string.IsNullOrWhiteSpace(apiKey)
                ? "OpenAI API key is not set. OpenAI categorization is unavailable."
                : "OpenAI API key is set. AI categorization will use OpenAI.");
            return 0;
        }

        if (args[1].Equals("set", StringComparison.OrdinalIgnoreCase))
        {
            var apiKey = args.Count > 2
                ? args[2]
                : ReadSecret("OpenAI API key: ");

            await credentialStore.SaveStringAsync(CredentialKeys.OpenAiApiKey, apiKey, cancellationToken).ConfigureAwait(false);
            System.Console.WriteLine("OpenAI API key saved for this app session.");
            return 0;
        }

        if (args[1].Equals("clear", StringComparison.OrdinalIgnoreCase))
        {
            await credentialStore.ClearAsync(CredentialKeys.OpenAiApiKey, cancellationToken).ConfigureAwait(false);
            System.Console.WriteLine("OpenAI API key cleared for this app session.");
            return 0;
        }

        System.Console.Error.WriteLine("Usage: ai-key status|set|clear");
        return 1;
    }

    private static void PrintCategoryReport(
        CategorySpendingReport report,
        TransactionQuery query)
    {
        System.Console.WriteLine(query.StartDate is null && query.EndDate is null
            ? "Spending by category"
            : $"Spending by category ({query.StartDate:yyyy-MM-dd} to {query.EndDate:yyyy-MM-dd})");
        System.Console.WriteLine();

        foreach (var line in report.Lines)
        {
            System.Console.WriteLine($"{line.CategoryName,-24} {line.TransactionCount,4} {line.Amount,12:0.00}");
        }

        System.Console.WriteLine();
        System.Console.WriteLine($"Total{"",-23} {report.TotalSpending,12:0.00}");
    }

    private static void PrintHelp()
    {
        System.Console.WriteLine("Spendnest console");
        System.Console.WriteLine();
        System.Console.WriteLine("Available now:");
        System.Console.WriteLine("  run");
        System.Console.WriteLine("  help");
        System.Console.WriteLine("  parse <csv-file>");
        System.Console.WriteLine("  import <csv-file> [--card <card-name>]");
        System.Console.WriteLine("  report");
        System.Console.WriteLine("  report <csv-file> [csv-file...]");
        System.Console.WriteLine("  report month <yyyy-mm>");
        System.Console.WriteLine("  ai-key status");
        System.Console.WriteLine("  ai-key set");
        System.Console.WriteLine("  ai-key clear");
        System.Console.WriteLine("  ai-report [csv-file...]");
        System.Console.WriteLine("  review list");
        System.Console.WriteLine("  review set <transaction-id> <category-id> [--remember]");
        System.Console.WriteLine("  review confirm <transaction-id> [--remember]");
        System.Console.WriteLine("  exit");
    }

    private static void PrintWarnings(IReadOnlyList<StatementParseWarning> warnings)
    {
        if (warnings.Count == 0)
        {
            return;
        }

        System.Console.WriteLine();
        System.Console.WriteLine("Warnings:");
        foreach (var warning in warnings)
        {
            var prefix = warning.SourceRowNumber is null ? "file" : $"row {warning.SourceRowNumber}";
            System.Console.WriteLine($"  {prefix}: {warning.Message}");
        }
    }

    private static StatementFileImportOptions ParseImportOptions(IEnumerable<string> args)
    {
        var values = args.ToArray();
        for (var index = 0; index < values.Length; index++)
        {
            if (values[index].Equals("--card", StringComparison.OrdinalIgnoreCase)
                && index + 1 < values.Length)
            {
                return new StatementFileImportOptions
                {
                    CardAccountName = values[index + 1]
                };
            }
        }

        return new StatementFileImportOptions();
    }

    private static string ReadSecret(string prompt)
    {
        System.Console.Write(prompt);
        var secret = string.Empty;

        while (true)
        {
            var key = System.Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                System.Console.WriteLine();
                return secret;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (secret.Length > 0)
                {
                    secret = secret[..^1];
                }

                continue;
            }

            secret += key.KeyChar;
        }
    }

    private static bool HasFlag(
        IReadOnlyList<string> args,
        string flag)
    {
        return args.Any(arg => arg.Equals(flag, StringComparison.OrdinalIgnoreCase));
    }

    private static string FormatCategory(int? categoryId)
    {
        if (categoryId is null)
        {
            return "Uncategorized";
        }

        return BuiltInCategories.All
            .FirstOrDefault(category => category.Id == categoryId.Value)
            ?.Name
            ?? categoryId.Value.ToString();
    }
}
