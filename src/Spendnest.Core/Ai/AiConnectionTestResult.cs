namespace Spendnest.Core.Ai;

/// <summary>
/// Describes the result of testing an AI provider connection.
/// </summary>
public sealed record AiConnectionTestResult(
    bool Succeeded,
    string Message);
