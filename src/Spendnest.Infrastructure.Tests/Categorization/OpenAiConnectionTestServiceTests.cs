namespace Spendnest.Infrastructure.Tests.Categorization;

using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Spendnest.Core.Ai;
using Spendnest.Core.Credentials;
using Spendnest.Infrastructure.Categorization;
using Spendnest.Infrastructure.Credentials;

public class OpenAiConnectionTestServiceTests
{
    [Fact]
    public async Task TestOpenAiAsync_ShouldSendTinyRequestWithProvidedApiKeyAndModel()
    {
        string? authorization = null;
        string? requestBody = null;
        var service = new OpenAiConnectionTestService(
            new InMemoryCredentialStore(),
            new HttpClient(new StubHttpMessageHandler(request =>
            {
                authorization = request.Headers.Authorization?.ToString();
                requestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
            })),
            new OpenAiCategorizerOptions());

        var result = await service.TestOpenAiAsync(
            new AiConnectionTestRequest("test-key", "gpt-test", TimeSpan.FromSeconds(5)),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        authorization.Should().Be("Bearer test-key");
        requestBody.Should().NotBeNull();
        using var json = JsonDocument.Parse(requestBody!);
        json.RootElement.GetProperty("model").GetString().Should().Be("gpt-test");
        json.RootElement.GetProperty("input").GetString().Should().Contain("ok");
    }

    [Fact]
    public async Task TestOpenAiAsync_ShouldUseSavedApiKeyWhenRequestKeyIsBlank()
    {
        string? authorization = null;
        var service = new OpenAiConnectionTestService(
            new InMemoryCredentialStore(new Dictionary<string, string?>
            {
                [CredentialKeys.OpenAiApiKey] = "saved-key"
            }),
            new HttpClient(new StubHttpMessageHandler(request =>
            {
                authorization = request.Headers.Authorization?.ToString();

                return new HttpResponseMessage(HttpStatusCode.OK);
            })),
            new OpenAiCategorizerOptions());

        var result = await service.TestOpenAiAsync(
            new AiConnectionTestRequest(null, "gpt-test", TimeSpan.FromSeconds(5)),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        authorization.Should().Be("Bearer saved-key");
    }

    [Fact]
    public async Task TestOpenAiAsync_ShouldReturnFailureWhenApiKeyIsMissing()
    {
        var service = new OpenAiConnectionTestService(
            new InMemoryCredentialStore(),
            new HttpClient(new ThrowingHttpMessageHandler()),
            new OpenAiCategorizerOptions());

        var result = await service.TestOpenAiAsync(
            new AiConnectionTestRequest(null, "gpt-test", TimeSpan.FromSeconds(5)),
            CancellationToken.None);

        result.Should().Be(new AiConnectionTestResult(false, "OpenAI API key is required."));
    }

    [Fact]
    public async Task TestOpenAiAsync_ShouldReturnFailureForOpenAiError()
    {
        var service = new OpenAiConnectionTestService(
            new InMemoryCredentialStore(),
            new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                ReasonPhrase = "Unauthorized",
                Content = new StringContent("{\"error\":\"bad key\"}", Encoding.UTF8, "application/json")
            })),
            new OpenAiCategorizerOptions());

        var result = await service.TestOpenAiAsync(
            new AiConnectionTestRequest("bad-key", "gpt-test", TimeSpan.FromSeconds(5)),
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("401 Unauthorized");
        result.Message.Should().Contain("bad key");
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

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("HTTP should not be called.");
        }
    }
}
