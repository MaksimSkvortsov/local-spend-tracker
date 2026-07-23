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

    public StatementFileImportService(
        IStatementParser parser,
        ITransactionRepository transactionRepository)
    {
        this.parser = parser;
        this.transactionRepository = transactionRepository;
    }

    public async Task<StatementFileImportResult> ImportAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        await using var stream = File.OpenRead(filePath);
        var parseResult = await parser.ParseAsync(stream, new StatementParseOptions(), cancellationToken).ConfigureAwait(false);

        var importedAtUtc = DateTimeOffset.UtcNow;
        var transactions = parseResult.Rows
            .Select(row => new Transaction
            {
                Id = Guid.NewGuid(),
                PostedDate = row.PostedDate,
                OriginalDescription = row.OriginalDescription,
                Amount = row.Amount,
                SourceRowNumber = row.SourceRowNumber,
                ImportedAtUtc = importedAtUtc
            })
            .ToArray();

        await transactionRepository.AddRangeAsync(transactions, cancellationToken).ConfigureAwait(false);

        return new StatementFileImportResult(
            filePath,
            parseResult.Rows.Count,
            transactions.Length,
            parseResult.FailedRowCount,
            transactions,
            parseResult.Warnings);
    }
}
