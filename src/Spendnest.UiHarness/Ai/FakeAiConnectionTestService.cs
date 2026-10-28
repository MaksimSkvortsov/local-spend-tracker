using Spendnest.Core.Ai;
using Spendnest.Core.Credentials;

namespace Spendnest.UiHarness.Ai;

/// <summary>
/// Stands in for the OpenAI connection test in browser tests: it applies the same input checks as the real
/// service and succeeds without calling OpenAI.
/// </summary>
public sealed class FakeAiConnectionTestService(ICredentialStore credentialStore) : IAiConnectionTestService
{
    public async Task<AiConnectionTestResult> TestOpenAiAsync(
        AiConnectionTestRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Model))
        {
            return new AiConnectionTestResult(false, "Choose a ChatGPT model.");
        }

        var apiKey = string.IsNullOrWhiteSpace(request.ApiKey)
            ? await credentialStore.GetStringAsync(CredentialKeys.OpenAiApiKey, cancellationToken)
            : request.ApiKey;

        return string.IsNullOrWhiteSpace(apiKey)
            ? new AiConnectionTestResult(false, "OpenAI API key is required.")
            : new AiConnectionTestResult(true, "OpenAI connection succeeded.");
    }
}
