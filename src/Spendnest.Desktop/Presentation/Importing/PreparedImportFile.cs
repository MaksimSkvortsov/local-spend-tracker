using Spendnest.Core.Importing;

namespace Spendnest.Desktop.Presentation.Importing;

public sealed record PreparedImportFile(
    string FileName,
    string LocalPath,
    StatementParseResult? Preview,
    string? DetectedCardName,
    string FallbackCardName,
    string? ErrorMessage);
