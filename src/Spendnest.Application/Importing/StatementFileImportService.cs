using Spendnest.Core.Accounts;
using Spendnest.Core.Importing;
using Spendnest.Core.Progress;
using Spendnest.Core.Transactions;

namespace Spendnest.Application.Importing;

/// <summary>
/// Parses a statement file and saves normalized transactions to a repository.
/// </summary>
public sealed class StatementFileImportService : IStatementFileImportService
{
    private readonly IStatementParser parser;
    private readonly ITransactionRepository transactionRepository;
    private readonly ICardAccountRepository cardAccountRepository;
    private readonly IStatementImportRepository statementImportRepository;
    private readonly IStatementFileReader fileReader;

    public StatementFileImportService(
        IStatementParser parser,
        ITransactionRepository transactionRepository,
        ICardAccountRepository cardAccountRepository,
        IStatementImportRepository statementImportRepository,
        IStatementFileReader fileReader)
    {
        this.parser = parser;
        this.transactionRepository = transactionRepository;
        this.cardAccountRepository = cardAccountRepository;
        this.statementImportRepository = statementImportRepository;
        this.fileReader = fileReader;
    }

    public async Task<StatementFileImportResult> ImportAsync(
        string filePath,
        StatementFileImportOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        options.Progress?.Report(new FileUploadProgress(
            FileUploadProgressStage.ReadingFile,
            "Reading file"));
        await using var statementFile = await fileReader.OpenReadAsync(filePath, cancellationToken).ConfigureAwait(false);
        var existingStatementImport = await statementImportRepository
            .GetByFileHashAsync(statementFile.FileHash, cancellationToken)
            .ConfigureAwait(false);
        if (existingStatementImport is not null)
        {
            throw new DuplicateStatementImportException(statementFile.FileName);
        }

        var cardAccountName = NormalizeCardAccountName(options.CardAccountName);
        var cardAccount = await cardAccountRepository
            .GetByNameAsync(cardAccountName, cancellationToken)
            .ConfigureAwait(false)
            ?? await cardAccountRepository.CreateAsync(cardAccountName, cancellationToken).ConfigureAwait(false);

        var importedAtUtc = DateTimeOffset.UtcNow;
        var statementImport = new StatementImport
        {
            CardAccountId = cardAccount.Id,
            FilePath = statementFile.FilePath,
            FileName = statementFile.FileName,
            FileHash = statementFile.FileHash,
            StartedAtUtc = importedAtUtc
        };
        await statementImportRepository.AddAsync(statementImport, cancellationToken).ConfigureAwait(false);

        try
        {
            options.Progress?.Report(new FileUploadProgress(
                FileUploadProgressStage.ParsingTransactions,
                "Parsing transactions"));
            var parseResult = await parser.ParseAsync(statementFile.Content, new StatementParseOptions(), cancellationToken).ConfigureAwait(false);

            var existingTransactions = await transactionRepository.ListAsync(cancellationToken).ConfigureAwait(false);
            var seenFingerprints = existingTransactions
                .Select(TransactionFingerprint.Create)
                .ToHashSet(StringComparer.Ordinal);
            var skippedDuplicateCount = 0;
            var transactions = parseResult.Rows
                .Select(row =>
                    new Transaction
                    {
                        Id = Guid.NewGuid(),
                        CardAccountId = cardAccount.Id,
                        StatementImportId = statementImport.Id,
                        PostedDate = row.PostedDate,
                        OriginalDescription = row.OriginalDescription,
                        Amount = row.Amount,
                        SourceRowNumber = row.SourceRowNumber,
                        ImportedAtUtc = importedAtUtc
                    })
                .Where(transaction =>
                {
                    var fingerprint = TransactionFingerprint.Create(transaction);
                    if (!seenFingerprints.Add(fingerprint))
                    {
                        skippedDuplicateCount++;
                        return false;
                    }

                    return true;
                })
                .ToArray();

            options.Progress?.Report(new FileUploadProgress(
                FileUploadProgressStage.SavingTransactions,
                "Saving transactions",
                transactions.Length,
                parseResult.Rows.Count));
            await transactionRepository.AddRangeAsync(transactions, cancellationToken).ConfigureAwait(false);

            statementImport.Status = StatementImportStatus.Completed;
            statementImport.ParsedRowCount = parseResult.Rows.Count;
            statementImport.SavedTransactionCount = transactions.Length;
            statementImport.SkippedDuplicateTransactionCount = skippedDuplicateCount;
            statementImport.FailedRowCount = parseResult.FailedRowCount;
            statementImport.CompletedAtUtc = DateTimeOffset.UtcNow;
            await statementImportRepository.UpdateAsync(statementImport, cancellationToken).ConfigureAwait(false);

            return new StatementFileImportResult(
                statementImport.Id,
                statementFile.FilePath,
                cardAccount.Id,
                cardAccount.Name,
                parseResult.Rows.Count,
                transactions.Length,
                skippedDuplicateCount,
                parseResult.FailedRowCount,
                transactions,
                parseResult.Warnings);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            statementImport.Status = StatementImportStatus.Failed;
            statementImport.ErrorMessage = exception.Message;
            statementImport.CompletedAtUtc = DateTimeOffset.UtcNow;
            await statementImportRepository.UpdateAsync(statementImport, CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private static string NormalizeCardAccountName(string cardAccountName)
    {
        return string.IsNullOrWhiteSpace(cardAccountName)
            ? "Default Card"
            : cardAccountName.Trim();
    }

}
