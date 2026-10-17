using Spendnest.Application.Importing;
using Spendnest.Core.Categories;
using Spendnest.Core.Importing;
using Spendnest.Core.Progress;
using Spendnest.Desktop.Presentation.Importing;

namespace Spendnest.Desktop.Services;

public sealed class ImportWorkflowService(
    IStatementFileImportService importService,
    StatementImportRetryService importRetryService)
{
    public async Task<ImportWorkflowResult> ImportAndCategorizeAsync(
        string filePath,
        string cardAccountName,
        IProgress<FileUploadProgress> progress,
        CancellationToken cancellationToken)
    {
        var importResult = await importService.ImportAsync(
            filePath,
            new StatementFileImportOptions
            {
                CardAccountName = cardAccountName,
                Progress = progress
            },
            cancellationToken);

        return CreateWorkflowResult(importResult, progress);
    }

    public async Task<ImportWorkflowResult> RetryAsync(
        Guid statementImportId,
        IProgress<FileUploadProgress> progress,
        CancellationToken cancellationToken)
    {
        var importResult = await importRetryService.RetryAsync(
            statementImportId,
            progress,
            cancellationToken);

        return CreateWorkflowResult(importResult, progress);
    }

    private static ImportWorkflowResult CreateWorkflowResult(
        StatementFileImportResult importResult,
        IProgress<FileUploadProgress> progress)
    {
        progress.Report(FileUploadProgress.RefreshingData);

        return new ImportWorkflowResult(
            importResult,
            importResult.Categorizations.ToDictionary(
                categorization => categorization.TransactionId,
                categorization => GetBuiltInCategoryName(categorization.CategoryId)),
            CategorizationHistorySummary.From(importResult.Categorizations),
            GetImportFocusDate(importResult));
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
