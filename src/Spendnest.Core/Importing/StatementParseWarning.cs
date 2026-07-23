namespace Spendnest.Core.Importing;

/// <summary>
/// Describes a non-fatal statement parsing issue.
/// </summary>
public sealed record StatementParseWarning(
    int? SourceRowNumber,
    string Message);
