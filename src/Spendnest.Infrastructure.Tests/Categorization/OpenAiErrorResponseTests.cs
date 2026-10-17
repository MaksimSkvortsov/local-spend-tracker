namespace Spendnest.Infrastructure.Tests.Categorization;

using FluentAssertions;
using Spendnest.Infrastructure.Categorization;

public class OpenAiErrorResponseTests
{
    [Fact]
    public void Parse_ShouldReadErrorCodeTypeAndMessage()
    {
        var error = OpenAiErrorResponse.Parse(
            429,
            """{"error":{"message":"You exceeded your current quota.","type":"insufficient_quota","param":null,"code":"insufficient_quota"}}""");

        error.Should().Be(new OpenAiErrorResponse(429, "insufficient_quota", "insufficient_quota", "You exceeded your current quota."));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("<html>Bad gateway</html>")]
    [InlineData("""{"message":"no error object"}""")]
    [InlineData("""{"error":"plain string"}""")]
    public void Parse_ShouldKeepOnlyStatusWhenBodyIsNotAnOpenAiError(string? body)
    {
        OpenAiErrorResponse.Parse(502, body).Should().Be(new OpenAiErrorResponse(502, null, null, null));
    }

    [Theory]
    [InlineData(429, "insufficient_quota", null, "OpenAI quota exceeded. Check the plan and billing for this API key.")]
    [InlineData(429, "rate_limit_exceeded", null, "OpenAI rate limit reached. Try again in a few minutes.")]
    [InlineData(429, null, null, "OpenAI rate limit reached. Try again in a few minutes.")]
    [InlineData(401, "invalid_api_key", null, "OpenAI rejected the API key.")]
    [InlineData(404, "model_not_found", null, "OpenAI model 'gpt-test' is not available for this API key.")]
    [InlineData(500, null, "The server had an error.", "OpenAI request failed with status 500: The server had an error.")]
    [InlineData(503, null, null, "OpenAI request failed with status 503.")]
    public void ToReason_ShouldExplainFailureInActionableTerms(
        int statusCode,
        string? code,
        string? message,
        string expected)
    {
        new OpenAiErrorResponse(statusCode, code, null, message).ToReason("gpt-test").Should().Be(expected);
    }
}
