using Spendnest.Core.Importing;
using Spendnest.Core.Progress;
using Spendnest.Desktop.Services;

namespace Spendnest.Desktop.Presentation.Importing;

public sealed class ImportSelectionDraft
{
    private const string DefaultFallbackCardName = "Imported Card";

    public string CardName { get; set; } = "Default Card";

    public string FallbackCardName { get; private set; } = DefaultFallbackCardName;

    public string SelectedExistingCardName { get; set; } = string.Empty;

    public string? FilePath { get; private set; }

    public string? FileName { get; private set; }

    public string? CardSelectionMessage { get; private set; }

    public string? ErrorMessage { get; set; }

    public bool CardDetected { get; private set; }

    public int PreviewPage { get; set; } = 1;

    public IReadOnlyList<StatementParseWarning> Warnings { get; set; } = [];

    public IReadOnlyList<PreviewRow> PreviewRows { get; set; } = [];

    public IReadOnlyDictionary<Guid, string> PreviewCategoryNamesByTransactionId { get; set; } =
        new Dictionary<Guid, string>();

    public bool HasFile => !string.IsNullOrWhiteSpace(FilePath);

    public string SelectedFileName => FileName ?? "No CSV selected";

    public string EffectiveCardName =>
        string.IsNullOrWhiteSpace(CardName)
            ? FallbackCardName
            : CardName.Trim();

    public bool CanImport(bool isBusy)
    {
        return !isBusy
            && HasFile
            && !string.IsNullOrWhiteSpace(EffectiveCardName);
    }

    public void PrepareForSelectedFile(PickedStatementFile file)
    {
        FileName = file.FileName;
        FilePath = file.LocalPath;
        Warnings = [];
        PreviewPage = 1;
        PreviewCategoryNamesByTransactionId = new Dictionary<Guid, string>();
    }

    public void ApplyPreparedFile(
        PreparedImportFile preparedFile,
        int availableCardCount)
    {
        PreviewRows = ImportPreviewRows.FromParseResult(preparedFile.Preview);
        ErrorMessage = preparedFile.ErrorMessage;
        FallbackCardName = preparedFile.FallbackCardName;

        if (!string.IsNullOrWhiteSpace(preparedFile.DetectedCardName))
        {
            CardName = preparedFile.DetectedCardName;
            CardDetected = true;
            SelectedExistingCardName = string.Empty;
            CardSelectionMessage = "Card detected from the selected CSV.";
            return;
        }

        SelectedExistingCardName = string.Empty;
        CardName = preparedFile.FallbackCardName;
        CardDetected = false;
        CardSelectionMessage = availableCardCount == 0
            ? "Card was not detected. Review this generated card name before importing."
            : "Card was not detected. Choose an existing card or review this generated card name.";
    }

    public void UseSelectedExistingCard()
    {
        CardName = string.IsNullOrWhiteSpace(SelectedExistingCardName)
            ? FallbackCardName
            : SelectedExistingCardName;
    }

    public void Clear(bool clearMessages)
    {
        FileName = null;
        FilePath = null;
        SelectedExistingCardName = string.Empty;
        PreviewPage = 1;
        Warnings = [];
        PreviewRows = [];
        PreviewCategoryNamesByTransactionId = new Dictionary<Guid, string>();

        if (clearMessages)
        {
            ErrorMessage = null;
        }

        CardSelectionMessage = null;
        CardDetected = false;
        FallbackCardName = DefaultFallbackCardName;
        CardName = string.Empty;
    }

    public bool IsCategorizing(FileUploadProgress? progress)
    {
        return progress?.Stage == FileUploadProgressStage.CategorizingWithAi;
    }
}
