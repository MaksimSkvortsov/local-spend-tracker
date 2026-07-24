using System.Text;

namespace Spendnest.Console;

/// <summary>
/// Splits console command text while preserving quoted file paths and names.
/// </summary>
public static class CommandLineTokenizer
{
    public static IReadOnlyList<string> Tokenize(string commandLine)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var character in commandLine)
        {
            if (character == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(character) && !inQuotes)
            {
                AddCurrentToken(tokens, current);
                continue;
            }

            current.Append(character);
        }

        AddCurrentToken(tokens, current);

        return tokens;
    }

    private static void AddCurrentToken(
        List<string> tokens,
        StringBuilder current)
    {
        if (current.Length == 0)
        {
            return;
        }

        tokens.Add(current.ToString());
        current.Clear();
    }
}
