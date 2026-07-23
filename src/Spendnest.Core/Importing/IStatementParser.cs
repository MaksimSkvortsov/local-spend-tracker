namespace Spendnest.Core.Importing;

/// <summary>
/// Parses a statement stream into normalized rows without persistence.
/// </summary>
public interface IStatementParser
{
    Task<StatementParseResult> ParseAsync(
        Stream stream,
        StatementParseOptions options,
        CancellationToken cancellationToken);
}
