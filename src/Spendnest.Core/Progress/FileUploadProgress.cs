namespace Spendnest.Core.Progress;

/// <summary>
/// Describes the current file upload step for host UI progress reporting.
/// </summary>
public sealed record FileUploadProgress(
    FileUploadProgressStage Stage,
    string Message,
    int? Current = null,
    int? Total = null)
{
    public static FileUploadProgress ReadingFile { get; } = new(
        FileUploadProgressStage.ReadingFile,
        "Reading file");

    public static FileUploadProgress ParsingTransactions { get; } = new(
        FileUploadProgressStage.ParsingTransactions,
        "Parsing transactions");

    public static FileUploadProgress RefreshingData { get; } = new(
        FileUploadProgressStage.RefreshingData,
        "Refreshing data");

    public static FileUploadProgress SavingTransactions(
        int current,
        int total)
    {
        return new FileUploadProgress(
            FileUploadProgressStage.SavingTransactions,
            "Saving transactions",
            current,
            total);
    }

    public static FileUploadProgress CategorizingWithAi(
        int current,
        int total)
    {
        return new FileUploadProgress(
            FileUploadProgressStage.CategorizingWithAi,
            "Categorizing with AI",
            current,
            total);
    }

    public static FileUploadProgress CategorizingTransactions(
        int current,
        int total)
    {
        return new FileUploadProgress(
            FileUploadProgressStage.CategorizingWithAi,
            "Categorizing transactions",
            current,
            total);
    }
}
