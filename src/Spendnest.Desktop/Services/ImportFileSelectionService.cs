using System.Text;
using System.Text.RegularExpressions;
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
        var detectedCardName = await DetectCardNameAsync(file.LocalPath, cancellationToken);

        return new PreparedImportFile(
            file.FileName,
            file.LocalPath,
            preview.Result,
            detectedCardName,
            BuildFallbackCardName(file.FileName),
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
        catch (Exception exception)
        {
            return new PreviewResult(null, exception.Message);
        }
    }

    private static async Task<string?> DetectCardNameAsync(
        string? filePath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return null;
        }

        await using var stream = File.OpenRead(filePath);
        using var reader = new StreamReader(stream);
        var headerLine = await reader.ReadLineAsync(cancellationToken);
        var firstDataLine = await reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(headerLine) || string.IsNullOrWhiteSpace(firstDataLine))
        {
            return null;
        }

        var headers = ParseCsvLine(headerLine);
        var values = ParseCsvLine(firstDataLine);
        var cardNumberIndex = headers.FindIndex(header =>
            header.Equals("Card No.", StringComparison.OrdinalIgnoreCase)
            || header.Equals("Card Number", StringComparison.OrdinalIgnoreCase));
        if (cardNumberIndex < 0 || cardNumberIndex >= values.Count)
        {
            return null;
        }

        var cardNumber = values[cardNumberIndex].Trim();
        return string.IsNullOrWhiteSpace(cardNumber)
            ? null
            : $"Capital One {cardNumber}";
    }

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append(character);
                    index++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (character == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(character);
        }

        values.Add(current.ToString());
        return values;
    }

    private static string BuildFallbackCardName(string? fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(stem))
        {
            return "Imported Card";
        }

        var normalized = Regex
            .Replace(stem, @"[_\-]+", " ")
            .Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? "Imported Card"
            : normalized;
    }

    private sealed record PreviewResult(
        StatementParseResult? Result,
        string? ErrorMessage);
}
