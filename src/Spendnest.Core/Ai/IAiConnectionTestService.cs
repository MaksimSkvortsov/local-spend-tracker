namespace Spendnest.Core.Ai;

/// <summary>
/// Validates that configured AI credentials can reach the provider.
/// </summary>
public interface IAiConnectionTestService
{
    Task<AiConnectionTestResult> TestOpenAiAsync(
        AiConnectionTestRequest request,
        CancellationToken cancellationToken);
}
