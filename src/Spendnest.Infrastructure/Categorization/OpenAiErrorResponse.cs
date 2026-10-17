using System.Text.Json;

namespace Spendnest.Infrastructure.Categorization;

/// <summary>
/// An OpenAI error response (<c>{"error": {"message", "type", "code"}}</c>) and the user-facing reason it maps to.
/// </summary>
public sealed record OpenAiErrorResponse(
    int StatusCode,
    string? Code,
    string? Type,
    string? Message)
{
    public static OpenAiErrorResponse Parse(int statusCode, string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return new OpenAiErrorResponse(statusCode, null, null, null);
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("error", out var error)
                || error.ValueKind != JsonValueKind.Object)
            {
                return new OpenAiErrorResponse(statusCode, null, null, null);
            }

            return new OpenAiErrorResponse(
                statusCode,
                ReadString(error, "code"),
                ReadString(error, "type"),
                ReadString(error, "message"));
        }
        catch (JsonException)
        {
            return new OpenAiErrorResponse(statusCode, null, null, null);
        }
    }

    /// <summary>
    /// Explains the failure in terms the user can act on. A 429 means either an exhausted quota or a rate limit;
    /// only the error code tells them apart.
    /// </summary>
    public string ToReason(string model)
    {
        if (Code is "insufficient_quota" or "credit_balance_exhausted" || Type is "insufficient_quota")
        {
            return "OpenAI quota exceeded. Check the plan and billing for this API key.";
        }

        if (StatusCode == 429)
        {
            return "OpenAI rate limit reached. Try again in a few minutes.";
        }

        if (StatusCode == 401 || Code is "invalid_api_key")
        {
            return "OpenAI rejected the API key.";
        }

        if (StatusCode == 404 || Code is "model_not_found")
        {
            return $"OpenAI model '{model}' is not available for this API key.";
        }

        return string.IsNullOrWhiteSpace(Message)
            ? $"OpenAI request failed with status {StatusCode}."
            : $"OpenAI request failed with status {StatusCode}: {Message}";
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}
