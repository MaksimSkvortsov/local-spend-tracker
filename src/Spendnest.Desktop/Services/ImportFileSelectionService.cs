using Spendnest.Application.Importing;
using Spendnest.Core.Importing;
using Spendnest.Desktop.Presentation.Importing;

namespace Spendnest.Desktop.Services;

public sealed class ImportFileSelectionService(IStatementParser statementParser)
{
    public async Task<PreparedImportFile> PrepareAsync(
        PickedStatementFile file,
        CancellationToken cancellationToken)
    {
        var preview = await PreviewSelectedFileAsync(file.LocalPath, cancellationToken);

        return new PreparedImportFile(
            file.FileName,
            file.LocalPath,
            preview.Result,
            StatementCardNameSuggestion.FromCardNumber(preview.Result?.CardNumber, preview.Result?.CardIssuer),
            StatementCardNameSuggestion.FromFileName(file.FileName),
            preview.ErrorMessage);
    }

    private async Task<PreviewResult> PreviewSelectedFileAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(filePath);
            var result = await statementParser.ParseAsync(
                stream,
                new StatementParseOptions(),
                cancellationToken);

            return new PreviewResult(result, null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new PreviewResult(null, exception.Message);
        }
    }

    private sealed record PreviewResult(
        StatementParseResult? Result,
        string? ErrorMessage);
}
