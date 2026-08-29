using Spendnest.Application.Categorization;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Importing;
using Spendnest.Core.Progress;
using Spendnest.Desktop.Presentation.Importing;

namespace Spendnest.Desktop.Services;

public sealed class ImportWorkflowService(
    IStatementFileImportService importService,
    ITransactionCategorizationService categorizationService,
    ITransactionCategorizationApplier categorizationApplier,
    IStatementImportRepository importHistoryRepository)
{
    public async Task<ImportWorkflowResult> ImportAndCategorizeAsync(
        string filePath,
        string cardAccountName,
        IProgress<FileUploadProgress> progress,
        CancellationToken cancellationToken)
    {
        StatementFileImportResult? importResult = null;

        try
        {
            importResult = await importService.ImportAsync(
                filePath,
                new StatementFileImportOptions
                {
                    CardAccountName = cardAccountName,
                    Progress = progress
                },
                cancellationToken);

            await UpdateStatementImportStatusAsync(
                importResult.StatementImportId,
                StatementImportStatus.Pending,
                null,
                cancellationToken);
            progress.Report(FileUploadProgress.CategorizingTransactions(
                0,
                importResult.SavedTransactions.Count));

            var categorizations = await categorizationService.CategorizeAsync(
                importResult.SavedTransactions,
                progress,
                cancellationToken);
            await categorizationApplier.ApplyAsync(categorizations, cancellationToken);

            await UpdateStatementImportStatusAsync(
                importResult.StatementImportId,
                StatementImportStatus.Completed,
                null,
                cancellationToken);
            progress.Report(FileUploadProgress.RefreshingData);

            return new ImportWorkflowResult(
                importResult,
                categorizations.ToDictionary(
                    categorization => categorization.TransactionId,
                    categorization => GetBuiltInCategoryName(categorization.CategoryId)),
                CategorizationHistorySummary.From(categorizations),
                GetImportFocusDate(importResult));
        }
        catch (Exception exception)
        {
            if (importResult is not null)
            {
                await UpdateStatementImportStatusAsync(
                    importResult.StatementImportId,
                    StatementImportStatus.Failed,
                    exception.Message,
                    cancellationToken);
            }

            throw;
        }
    }

    private async Task UpdateStatementImportStatusAsync(
        Guid statementImportId,
        StatementImportStatus status,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        var statementImport = (await importHistoryRepository.ListAsync(cancellationToken))
            .FirstOrDefault(import => import.Id == statementImportId);
        if (statementImport is null)
        {
            return;
        }

        statementImport.Status = status;
        statementImport.ErrorMessage = errorMessage;
        if (status is StatementImportStatus.Completed or StatementImportStatus.Failed)
        {
            statementImport.CompletedAtUtc = DateTimeOffset.UtcNow;
        }

        await importHistoryRepository.UpdateAsync(statementImport, cancellationToken);
    }

    private static DateOnly? GetImportFocusDate(StatementFileImportResult importResult)
    {
        return importResult.SavedTransactions
            .OrderByDescending(transaction => transaction.PostedDate)
            .Select(transaction => (DateOnly?)transaction.PostedDate)
            .FirstOrDefault();
    }

    private static string GetBuiltInCategoryName(int categoryId)
    {
        return BuiltInCategories.All.FirstOrDefault(category => category.Id == categoryId)?.Name
            ?? "Other";
    }
}
