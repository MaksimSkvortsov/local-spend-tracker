using Spendnest.Core.Accounts;
using Spendnest.Core.Importing;
using Spendnest.Core.Progress;

namespace Spendnest.Application.Importing;

/// <summary>
/// Imports a failed statement import's file again onto the same card, replacing the failed history row.
/// </summary>
public sealed class StatementImportRetryService(
    IStatementFileImportService importService,
    IStatementImportRepository statementImportRepository,
    ICardAccountRepository cardAccountRepository)
{
    public async Task<StatementFileImportResult> RetryAsync(
        Guid statementImportId,
        IProgress<FileUploadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var failedImport = await statementImportRepository.GetByIdAsync(statementImportId, cancellationToken)
            ?? throw new InvalidOperationException("Import was not found.");
        if (failedImport.Status != StatementImportStatus.Failed)
        {
            throw new InvalidOperationException("Only failed imports can be retried.");
        }

        var cards = await cardAccountRepository.ListAsync(cancellationToken);
        var card = cards.FirstOrDefault(item => item.Id == failedImport.CardAccountId)
            ?? throw new InvalidOperationException("The card for this import no longer exists. Import the file again from the Import page.");

        try
        {
            return await importService.ImportAsync(
                failedImport.FilePath,
                new StatementFileImportOptions
                {
                    CardAccountName = card.Name,
                    Progress = progress
                },
                cancellationToken);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new InvalidOperationException(
                $"File not found at {failedImport.FilePath}. Import it again from the Import page.",
                exception);
        }
        finally
        {
            await DeleteWhenReplacedAsync(failedImport);
        }
    }

    /// <summary>
    /// Deletes the old failed row once the attempt recorded a newer import of the same file, so history keeps
    /// one row per file. Attempts that failed before recording an import keep the old row.
    /// </summary>
    private async Task DeleteWhenReplacedAsync(StatementImport failedImport)
    {
        var imports = await statementImportRepository.ListAsync(CancellationToken.None);
        var wasReplaced = imports.Any(statementImport =>
            statementImport.Id != failedImport.Id
            && string.Equals(statementImport.FilePath, failedImport.FilePath, StringComparison.OrdinalIgnoreCase)
            && statementImport.StartedAtUtc >= failedImport.StartedAtUtc);
        if (wasReplaced)
        {
            await statementImportRepository.DeleteAsync(failedImport.Id, CancellationToken.None);
        }
    }
}
