using Spendnest.Core.Importing;

namespace Spendnest.Desktop.Presentation.Importing;

public sealed record ImportWorkflowResult(
    StatementFileImportResult ImportResult,
    IReadOnlyDictionary<Guid, string> PreviewCategoryNamesByTransactionId,
    CategorizationHistorySummary CategorizationSummary,
    DateOnly? FocusDate);
