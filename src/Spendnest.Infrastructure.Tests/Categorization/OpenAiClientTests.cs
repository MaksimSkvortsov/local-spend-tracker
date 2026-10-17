namespace Spendnest.Infrastructure.Tests.Categorization;

using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Spendnest.Core.Categorization;
using Spendnest.Infrastructure.Categorization;

public class OpenAiClientTests
{
    [Fact]
    public async Task SendResponsesRequestAsync_ShouldSendAuthorizedJsonRequestAndReadOutputText()
    {
        string? authorization = null;
        string? requestBody = null;
        var client = new OpenAiClient(
            new HttpClient(new StubHttpMessageHandler(request =>
            {
                authorization = request.Headers.Authorization?.ToString();
                requestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { output_text = "ok" }),
                        Encoding.UTF8,
                        "application/json")
                };
            })),
            new OpenAiCategorizerOptions
            {
                ApiKey = "test-key"
            });

        var result = await client.SendResponsesRequestAsync(new { model = "test-model" }, CancellationToken.None);

        result.Should().Be("ok");
        authorization.Should().Be("Bearer test-key");
        requestBody.Should().Contain("test-model");
    }

    [Fact]
    public async Task SendResponsesRequestAsync_ShouldReadNestedOutputText()
    {
        var client = new OpenAiClient(
            new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        output = new[]
                        {
                            new
                            {
                                content = new[]
                                {
                                    new { text = "nested ok" }
                                }
                            }
                        }
                    }),
                    Encoding.UTF8,
                    "application/json")
            })),
            new OpenAiCategorizerOptions
            {
                ApiKey = "test-key"
            });

        var result = await client.SendResponsesRequestAsync(new { model = "test-model" }, CancellationToken.None);

        result.Should().Be("nested ok");
    }

    [Fact]
    public async Task SendResponsesRequestAsync_ShouldReportOpenAiErrorReasonWhenRequestFails()
    {
        var client = new OpenAiClient(
            new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent(
                    """{"error":{"message":"You exceeded your current quota.","type":"insufficient_quota","code":"insufficient_quota"}}""",
                    Encoding.UTF8,
                    "application/json")
            })),
            new OpenAiCategorizerOptions
            {
                ApiKey = "test-key"
            });

        var act = () => client.SendResponsesRequestAsync(new { model = "test-model" }, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<AiCategorizationUnavailableException>()
            .WithMessage("OpenAI quota exceeded. Check the plan and billing for this API key.");
        exception.Which.InnerException.Should().BeOfType<HttpRequestException>()
            .Which.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> send;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> send)
        {
            this.send = send;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(send(request));
        }
    }
}
