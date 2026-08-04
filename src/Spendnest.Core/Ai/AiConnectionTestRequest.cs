namespace Spendnest.Core.Ai;

/// <summary>
/// Captures the OpenAI settings to validate with a lightweight request.
/// </summary>
public sealed record AiConnectionTestRequest(
    string? ApiKey,
    string Model,
    TimeSpan RequestTimeout);
