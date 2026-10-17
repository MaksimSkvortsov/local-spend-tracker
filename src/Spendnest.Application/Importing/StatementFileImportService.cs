using Spendnest.Core.Accounts;
using Spendnest.Core.Categorization;
using Spendnest.Core.Importing;
using Spendnest.Core.Progress;
using Spendnest.Core.Transactions;

namespace Spendnest.Application.Importing;

/// <summary>
/// Parses and categorizes a statement file, then saves the import, its transactions, and their
/// category assignments together. When any new transaction stays unresolved, the import is failed
/// and no transactions are saved.
/// </summary>
public sealed class StatementFileImportService : IStatementFileImportService
{
    private readonly IStatementParser parser;
    private readonly ITransactionRepository transactionRepository;
    private readonly ICardAccountRepository cardAccountRepository;
    private readonly IStatementImportRepository statementImportRepository;
    private readonly IStatementImportStore statementImportStore;
    private readonly ITransactionCategorizationService categorizationService;
    private readonly IStatementFileReader fileReader;

    public StatementFileImportService(
        IStatementParser parser,
        ITransactionRepository transactionRepository,
        ICardAccountRepository cardAccountRepository,
        IStatementImportRepository statementImportRepository,
        IStatementImportStore statementImportStore,
        ITransactionCategorizationService categorizationService,
        IStatementFileReader fileReader)
    {
        this.parser = parser;
        this.transactionRepository = transactionRepository;
        this.cardAccountRepository = cardAccountRepository;
        this.statementImportRepository = statementImportRepository;
        this.statementImportStore = statementImportStore;
        this.categorizationService = categorizationService;
        this.fileReader = fileReader;
    }

    public async Task<StatementFileImportResult> ImportAsync(
        string filePath,
        StatementFileImportOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        options.Progress?.Report(FileUploadProgress.ReadingFile);
        await using var statementFile = await fileReader.OpenReadAsync(filePath, cancellationToken).ConfigureAwait(false);
        await EnsureStatementWasNotAlreadyImportedAsync(statementFile, cancellationToken).ConfigureAwait(false);

        var parseResult = await ParseStatementAsync(statementFile, options.Progress, cancellationToken).ConfigureAwait(false);
        if (!parseResult.HasTransaction)
        {
            var error = string.IsNullOrWhiteSpace(parseResult.Error)
                ? "No transactions could be parsed from the selected file."
                : parseResult.Error;
            throw new InvalidOperationException(error);
        }

        var cardAccount = await GetOrCreateCardAccountAsync(options.CardAccountName, cancellationToken).ConfigureAwait(false);
        var importedAtUtc = DateTimeOffset.UtcNow;
        var statementImport = CreateStatementImport(statementFile, cardAccount.Id, importedAtUtc);
        await statementImportRepository.AddAsync(statementImport, cancellationToken).ConfigureAwait(false);

        try
        {
            var preparedTransactions = await PrepareTransactionsAsync(
                parseResult.Rows,
                cardAccount.Id,
                statementImport.Id,
                importedAtUtc,
                cancellationToken).ConfigureAwait(false);

            var categorizations = await CategorizeTransactionsAsync(
                preparedTransactions.Transactions,
                options.Progress,
                cancellationToken).ConfigureAwait(false);

            options.Progress?.Report(FileUploadProgress.SavingTransactions(
                preparedTransactions.Transactions.Count,
                parseResult.Rows.Count));
            CompleteStatementImport(statementImport, parseResult, preparedTransactions);
            var assignedAtUtc = DateTimeOffset.UtcNow;
            await statementImportStore.CompleteImportAsync(
                statementImport,
                preparedTransactions.Transactions,
                categorizations.Select(categorization => categorization.ToAssignment(assignedAtUtc)).ToArray(),
                cancellationToken).ConfigureAwait(false);

            return CreateImportResult(
                statementImport,
                statementFile,
                cardAccount,
                parseResult,
                preparedTransactions,
                categorizations);
        }
        catch (OperationCanceledException)
        {
            FailStatementImport(statementImport, "Import was cancelled.");
            await statementImportRepository.UpdateAsync(statementImport, CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        catch (Exception exception)
        {
            FailStatementImport(statementImport, exception.Message);
            await statementImportRepository.UpdateAsync(statementImport, CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private async Task<IReadOnlyList<TransactionCategorization>> CategorizeTransactionsAsync(
        IReadOnlyList<Transaction> transactions,
        IProgress<FileUploadProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (transactions.Count == 0)
        {
            return [];
        }

        progress?.Report(FileUploadProgress.CategorizingTransactions(0, transactions.Count));
        var categorizations = await categorizationService
            .CategorizeAsync(transactions, progress, cancellationToken)
            .ConfigureAwait(false);

        var unresolved = categorizations
            .Where(categorization => categorization.Source == CategorizationSource.Unresolved)
            .ToArray();
        if (unresolved.Length > 0)
        {
            throw new StatementCategorizationFailedException(
                unresolved.Length,
                transactions.Count,
                unresolved.Select(categorization => categorization.Explanation).Distinct(StringComparer.Ordinal).ToArray());
        }

        return categorizations;
    }

    private async Task EnsureStatementWasNotAlreadyImportedAsync(
        StatementFileReadResult statementFile,
        CancellationToken cancellationToken)
    {
        var existingStatementImport = await statementImportRepository
            .GetByFileHashAsync(statementFile.FileHash, cancellationToken)
            .ConfigureAwait(false);
        if (existingStatementImport is not null)
        {
            throw new DuplicateStatementImportException(statementFile.FileName);
        }
    }

    private async Task<CardAccount> GetOrCreateCardAccountAsync(
        string cardAccountName,
        CancellationToken cancellationToken)
    {
        var normalizedCardAccountName = NormalizeCardAccountName(cardAccountName);

        return await cardAccountRepository
            .GetByNameAsync(normalizedCardAccountName, cancellationToken)
            .ConfigureAwait(false)
            ?? await cardAccountRepository.CreateAsync(normalizedCardAccountName, cancellationToken).ConfigureAwait(false);
    }

    private static StatementImport CreateStatementImport(
        StatementFileReadResult statementFile,
        Guid cardAccountId,
        DateTimeOffset startedAtUtc)
    {
        return new StatementImport
        {
            CardAccountId = cardAccountId,
            FilePath = statementFile.FilePath,
            FileName = statementFile.FileName,
            FileHash = statementFile.FileHash,
            StartedAtUtc = startedAtUtc
        };
    }

    private async Task<StatementParseResult> ParseStatementAsync(
        StatementFileReadResult statementFile,
        IProgress<FileUploadProgress>? progress,
        CancellationToken cancellationToken)
    {
        progress?.Report(FileUploadProgress.ParsingTransactions);

        return await parser
            .ParseAsync(statementFile.Content, new StatementParseOptions(), cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<PreparedTransactions> PrepareTransactionsAsync(
        IReadOnlyList<ParsedStatementRow> rows,
        Guid cardAccountId,
        Guid statementImportId,
        DateTimeOffset importedAtUtc,
        CancellationToken cancellationToken)
    {
        var existingTransactions = await transactionRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        var seenFingerprints = existingTransactions
            .Select(TransactionFingerprint.Create)
            .ToHashSet(StringComparer.Ordinal);
        var skippedDuplicateCount = 0;
        var transactions = new List<Transaction>(rows.Count);

        foreach (var row in rows)
        {
            var transaction = CreateTransaction(row, cardAccountId, statementImportId, importedAtUtc);
            var fingerprint = TransactionFingerprint.Create(transaction);
            if (!seenFingerprints.Add(fingerprint))
            {
                skippedDuplicateCount++;
                continue;
            }

            transactions.Add(transaction);
        }

        return new PreparedTransactions(transactions.ToArray(), skippedDuplicateCount);
    }

    private static Transaction CreateTransaction(
        ParsedStatementRow row,
        Guid cardAccountId,
        Guid statementImportId,
        DateTimeOffset importedAtUtc)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            CardAccountId = cardAccountId,
            StatementImportId = statementImportId,
            PostedDate = row.PostedDate,
            OriginalDescription = row.OriginalDescription,
            Amount = row.Amount,
            SourceRowNumber = row.SourceRowNumber,
            ImportedAtUtc = importedAtUtc
        };
    }

    private static void CompleteStatementImport(
        StatementImport statementImport,
        StatementParseResult parseResult,
        PreparedTransactions preparedTransactions)
    {
        statementImport.Status = StatementImportStatus.Completed;
        statementImport.ParsedRowCount = parseResult.Rows.Count;
        statementImport.SavedTransactionCount = preparedTransactions.Transactions.Count;
        statementImport.SkippedDuplicateTransactionCount = preparedTransactions.SkippedDuplicateCount;
        statementImport.FailedRowCount = parseResult.FailedRowCount;
        statementImport.CompletedAtUtc = DateTimeOffset.UtcNow;
    }

    private static void FailStatementImport(
        StatementImport statementImport,
        string errorMessage)
    {
        statementImport.Status = StatementImportStatus.Failed;
        statementImport.SavedTransactionCount = 0;
        statementImport.ErrorMessage = errorMessage;
        statementImport.CompletedAtUtc = DateTimeOffset.UtcNow;
    }

    private static StatementFileImportResult CreateImportResult(
        StatementImport statementImport,
        StatementFileReadResult statementFile,
        CardAccount cardAccount,
        StatementParseResult parseResult,
        PreparedTransactions preparedTransactions,
        IReadOnlyList<TransactionCategorization> categorizations)
    {
        return new StatementFileImportResult(
            statementImport.Id,
            statementFile.FilePath,
            cardAccount.Id,
            cardAccount.Name,
            parseResult.Rows.Count,
            preparedTransactions.Transactions.Count,
            preparedTransactions.SkippedDuplicateCount,
            parseResult.FailedRowCount,
            preparedTransactions.Transactions,
            parseResult.Warnings,
            categorizations);
    }

    private static string NormalizeCardAccountName(string cardAccountName)
    {
        return string.IsNullOrWhiteSpace(cardAccountName)
            ? "Default Card"
            : cardAccountName.Trim();
    }

    private sealed record PreparedTransactions(
        IReadOnlyList<Transaction> Transactions,
        int SkippedDuplicateCount);
}
