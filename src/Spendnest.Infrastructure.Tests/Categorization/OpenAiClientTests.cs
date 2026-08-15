namespace Spendnest.Infrastructure.Tests.Categorization;

using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
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
