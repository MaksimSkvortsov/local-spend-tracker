using Spendnest.Core.Categorization;
using Spendnest.Core.Importing;

namespace Spendnest.Application.Importing;

public sealed record StatementFolderImportResult(
    string CardName,
    IReadOnlyList<StatementFolderFileResult> Files,
    IReadOnlyList<TransactionCategorization> Categorizations);

public sealed record StatementFolderFileResult(
    string FilePath,
    StatementFolderFileStatus Status,
    StatementFileImportResult? Result,
    string? Error);

public enum StatementFolderFileStatus
{
    Imported,
    AlreadyImported,
    Failed
}
