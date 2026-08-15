using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Spendnest.Infrastructure.Categorization;

public sealed class OpenAiClient
{
    private readonly HttpClient httpClient;
    private readonly OpenAiCategorizerOptions options;
    private readonly ILogger<OpenAiClient> logger;

    public OpenAiClient(
        HttpClient httpClient,
        OpenAiCategorizerOptions options,
        ILogger<OpenAiClient>? logger = null)
    {
        this.httpClient = httpClient;
        this.options = options;
        this.logger = logger ?? NullLogger<OpenAiClient>.Instance;
    }

    public async Task<string> SendResponsesRequestAsync(
        object requestBody,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, options.Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody, OpenAiJson.Options),
            Encoding.UTF8,
            "application/json");

        var stopwatch = Stopwatch.StartNew();
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        logger.LogInformation(
            "Received OpenAI response with status {StatusCode} in {ElapsedMilliseconds} ms.",
            (int)response.StatusCode,
            stopwatch.ElapsedMilliseconds);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "OpenAI request failed with status {StatusCode} {ReasonPhrase}.",
                (int)response.StatusCode,
                response.ReasonPhrase);
        }

        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);

        return ReadOutputText(document.RootElement);
    }

    private static string ReadOutputText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var outputTextElement))
        {
            return outputTextElement.GetString() ?? string.Empty;
        }

        if (root.TryGetProperty("output", out var outputElement))
        {
            foreach (var outputItem in outputElement.EnumerateArray())
            {
                if (!outputItem.TryGetProperty("content", out var contentElement))
                {
                    continue;
                }

                foreach (var contentItem in contentElement.EnumerateArray())
                {
                    if (contentItem.TryGetProperty("text", out var textElement))
                    {
                        return textElement.GetString() ?? string.Empty;
                    }
                }
            }
        }

        throw new InvalidOperationException("OpenAI response did not include output text.");
    }
}
