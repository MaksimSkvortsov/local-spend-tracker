namespace Spendnest.Core.Progress;

/// <summary>
/// Describes the current file upload step for host UI progress reporting.
/// </summary>
public sealed record FileUploadProgress(
    FileUploadProgressStage Stage,
    string Message,
    int? Current = null,
    int? Total = null);
