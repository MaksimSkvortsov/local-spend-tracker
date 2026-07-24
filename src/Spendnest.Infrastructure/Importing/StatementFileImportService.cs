using Spendnest.Core.Accounts;
using Spendnest.Core.Importing;
using Spendnest.Core.Transactions;

namespace Spendnest.Infrastructure.Importing;

/// <summary>
/// Parses a statement file and saves normalized transactions to a repository.
/// </summary>
public sealed class StatementFileImportService : IStatementFileImportService
{
    private readonly IStatementParser parser;
    private readonly ITransactionRepository transactionRepository;
    private readonly ICardAccountRepository cardAccountRepository;

    public StatementFileImportService(
        IStatementParser parser,
        ITransactionRepository transactionRepository,
        ICardAccountRepository cardAccountRepository)
    {
        this.parser = parser;
        this.transactionRepository = transactionRepository;
        this.cardAccountRepository = cardAccountRepository;
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

        var cardAccountName = NormalizeCardAccountName(options.CardAccountName);
        var cardAccount = await cardAccountRepository
            .GetByNameAsync(cardAccountName, cancellationToken)
            .ConfigureAwait(false)
            ?? await cardAccountRepository.CreateAsync(cardAccountName, cancellationToken).ConfigureAwait(false);
        await using var stream = File.OpenRead(filePath);
        var parseResult = await parser.ParseAsync(stream, new StatementParseOptions(), cancellationToken).ConfigureAwait(false);

        var importedAtUtc = DateTimeOffset.UtcNow;
        var existingTransactions = await transactionRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        var seenFingerprints = existingTransactions
            .Select(TransactionFingerprint.Create)
            .ToHashSet(StringComparer.Ordinal);
        var skippedDuplicateCount = 0;
        var transactions = parseResult.Rows
            .Select(row => new Transaction
            {
                Id = Guid.NewGuid(),
                CardAccountId = cardAccount.Id,
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

        await transactionRepository.AddRangeAsync(transactions, cancellationToken).ConfigureAwait(false);

        return new StatementFileImportResult(
            filePath,
            cardAccount.Id,
            cardAccount.Name,
            parseResult.Rows.Count,
            transactions.Length,
            skippedDuplicateCount,
            parseResult.FailedRowCount,
            transactions,
            parseResult.Warnings);
    }

    private static string NormalizeCardAccountName(string cardAccountName)
    {
        return string.IsNullOrWhiteSpace(cardAccountName)
            ? "Default Card"
            : cardAccountName.Trim();
    }
}
