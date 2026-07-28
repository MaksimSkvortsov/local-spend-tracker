namespace Spendnest.Core.Progress;

/// <summary>
/// Long-running file upload stages that hosts can surface to users.
/// </summary>
public enum FileUploadProgressStage
{
    ReadingFile,
    ParsingTransactions,
    SavingTransactions,
    CategorizingWithAi,
    RefreshingData
}
