using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    private readonly ILogger<OpenAiConnectionTestService> logger;

    public OpenAiConnectionTestService(
        ICredentialStore credentialStore,
        HttpClient httpClient,
        OpenAiCategorizerOptions options,
        ILogger<OpenAiConnectionTestService>? logger = null)
    {
        this.credentialStore = credentialStore;
        this.httpClient = httpClient;
        this.options = options;
        this.logger = logger ?? NullLogger<OpenAiConnectionTestService>.Instance;
    }

    public async Task<AiConnectionTestResult> TestOpenAiAsync(
        AiConnectionTestRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.Model))
        {
            logger.LogWarning("OpenAI connection test skipped because no model is selected.");
            return new AiConnectionTestResult(false, "Choose a ChatGPT model.");
        }

        var model = request.Model.Trim();
        var apiKey = string.IsNullOrWhiteSpace(request.ApiKey)
            ? await credentialStore.GetStringAsync(CredentialKeys.OpenAiApiKey, cancellationToken).ConfigureAwait(false)
            : request.ApiKey.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("OpenAI connection test skipped because no API key is configured.");
            return new AiConnectionTestResult(false, "OpenAI API key is required.");
        }

        var requestTimeout = request.RequestTimeout > TimeSpan.Zero
            ? request.RequestTimeout
            : options.RequestTimeout;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(requestTimeout);

        try
        {
            logger.LogInformation(
                "Sending OpenAI connection test using model {Model}.",
                model);
            var stopwatch = Stopwatch.StartNew();
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, options.Endpoint);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(CreateRequestBody(model), JsonOptions),
                Encoding.UTF8,
                "application/json");

            using var response = await httpClient.SendAsync(httpRequest, timeout.Token).ConfigureAwait(false);
            logger.LogInformation(
                "Received OpenAI connection test response with status {StatusCode} in {ElapsedMilliseconds} ms.",
                (int)response.StatusCode,
                stopwatch.ElapsedMilliseconds);
            if (response.IsSuccessStatusCode)
            {
                return new AiConnectionTestResult(true, "OpenAI connection succeeded.");
            }

            var responseText = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
            logger.LogWarning(
                "OpenAI connection test failed with status {StatusCode} {ReasonPhrase}.",
                (int)response.StatusCode,
                response.ReasonPhrase);
            return new AiConnectionTestResult(
                false,
                $"OpenAI returned {(int)response.StatusCode} {response.ReasonPhrase}: {TrimResponse(responseText)}");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && timeout.IsCancellationRequested)
        {
            logger.LogWarning(
                "OpenAI connection test timed out after {TimeoutSeconds} seconds.",
                requestTimeout.TotalSeconds);
            return new AiConnectionTestResult(false, "OpenAI connection timed out.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "OpenAI connection test failed before a successful response was received.");
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
