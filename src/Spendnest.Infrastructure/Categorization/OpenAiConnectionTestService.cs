using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Spendnest.Core.Ai;
using Spendnest.Core.Credentials;

namespace Spendnest.Infrastructure.Categorization;

/// <summary>
/// Sends a minimal OpenAI Responses API request to verify credentials and model settings.
/// </summary>
public sealed class OpenAiConnectionTestService : IAiConnectionTestService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ICredentialStore credentialStore;
    private readonly HttpClient httpClient;
    private readonly OpenAiCategorizerOptions options;

    public OpenAiConnectionTestService(
        ICredentialStore credentialStore,
        HttpClient httpClient,
        OpenAiCategorizerOptions options)
    {
        this.credentialStore = credentialStore;
        this.httpClient = httpClient;
        this.options = options;
    }

    public async Task<AiConnectionTestResult> TestOpenAiAsync(
        AiConnectionTestRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.Model))
        {
            return new AiConnectionTestResult(false, "Choose a ChatGPT model.");
        }

        var model = request.Model.Trim();
        var apiKey = string.IsNullOrWhiteSpace(request.ApiKey)
            ? await credentialStore.GetStringAsync(CredentialKeys.OpenAiApiKey, cancellationToken).ConfigureAwait(false)
            : request.ApiKey.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new AiConnectionTestResult(false, "OpenAI API key is required.");
        }

        var requestTimeout = request.RequestTimeout > TimeSpan.Zero
            ? request.RequestTimeout
            : options.RequestTimeout;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(requestTimeout);

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, options.Endpoint);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(CreateRequestBody(model), JsonOptions),
                Encoding.UTF8,
                "application/json");

            using var response = await httpClient.SendAsync(httpRequest, timeout.Token).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return new AiConnectionTestResult(true, "OpenAI connection succeeded.");
            }

            var responseText = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
            return new AiConnectionTestResult(
                false,
                $"OpenAI returned {(int)response.StatusCode} {response.ReasonPhrase}: {TrimResponse(responseText)}");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && timeout.IsCancellationRequested)
        {
            return new AiConnectionTestResult(false, "OpenAI connection timed out.");
        }
        catch (HttpRequestException ex)
        {
            return new AiConnectionTestResult(false, $"OpenAI connection failed: {ex.Message}");
        }
    }

    private static object CreateRequestBody(string model)
    {
        return new
        {
            model,
            input = "Reply with exactly: ok",
            max_output_tokens = 16
        };
    }

    private static string TrimResponse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "No response body.";
        }

        var normalized = value.ReplaceLineEndings(" ").Trim();

        return normalized.Length <= 240
            ? normalized
            : $"{normalized[..240]}...";
    }
}
