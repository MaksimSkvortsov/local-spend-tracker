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
    public async Task TestOpenAiAsync_ShouldReturnFailureWhenModelIsBlank()
    {
        var service = new OpenAiConnectionTestService(
            new InMemoryCredentialStore(new Dictionary<string, string?>
            {
                [CredentialKeys.OpenAiApiKey] = "saved-key"
            }),
            new HttpClient(new ThrowingHttpMessageHandler()),
            new OpenAiCategorizerOptions());

        var result = await service.TestOpenAiAsync(
            new AiConnectionTestRequest("test-key", " ", TimeSpan.FromSeconds(5)),
            CancellationToken.None);

        result.Should().Be(new AiConnectionTestResult(false, "Choose a ChatGPT model."));
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

    [Fact]
    public async Task TestOpenAiAsync_ShouldTrimLongOpenAiErrorBody()
    {
        var service = new OpenAiConnectionTestService(
            new InMemoryCredentialStore(),
            new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                ReasonPhrase = "Bad Request",
                Content = new StringContent(
                    $"{new string('a', 120)}{Environment.NewLine}{new string('b', 160)}",
                    Encoding.UTF8,
                    "application/json")
            })),
            new OpenAiCategorizerOptions());

        var result = await service.TestOpenAiAsync(
            new AiConnectionTestRequest("bad-key", "gpt-test", TimeSpan.FromSeconds(5)),
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().StartWith("OpenAI returned 400 Bad Request: ");
        result.Message.Should().EndWith("...");
        result.Message.Should().NotContain(Environment.NewLine);
    }

    [Fact]
    public async Task TestOpenAiAsync_ShouldReturnNoResponseBodyWhenOpenAiErrorBodyIsBlank()
    {
        var service = new OpenAiConnectionTestService(
            new InMemoryCredentialStore(),
            new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                ReasonPhrase = "Bad Request",
                Content = new StringContent(" ")
            })),
            new OpenAiCategorizerOptions());

        var result = await service.TestOpenAiAsync(
            new AiConnectionTestRequest("bad-key", "gpt-test", TimeSpan.FromSeconds(5)),
            CancellationToken.None);

        result.Should().Be(new AiConnectionTestResult(
            false,
            "OpenAI returned 400 Bad Request: No response body."));
    }

    [Fact]
    public async Task TestOpenAiAsync_ShouldReturnFailureForHttpRequestException()
    {
        var service = new OpenAiConnectionTestService(
            new InMemoryCredentialStore(),
            new HttpClient(new HttpRequestExceptionMessageHandler()),
            new OpenAiCategorizerOptions());

        var result = await service.TestOpenAiAsync(
            new AiConnectionTestRequest("test-key", "gpt-test", TimeSpan.FromSeconds(5)),
            CancellationToken.None);

        result.Should().Be(new AiConnectionTestResult(
            false,
            "OpenAI connection failed: network unavailable"));
    }

    [Fact]
    public async Task TestOpenAiAsync_ShouldReturnFailureWhenRequestTimesOut()
    {
        var service = new OpenAiConnectionTestService(
            new InMemoryCredentialStore(),
            new HttpClient(new DelayingHttpMessageHandler(TimeSpan.FromSeconds(5))),
            new OpenAiCategorizerOptions
            {
                RequestTimeout = TimeSpan.FromMilliseconds(10)
            });

        var result = await service.TestOpenAiAsync(
            new AiConnectionTestRequest("test-key", "gpt-test", TimeSpan.Zero),
            CancellationToken.None);

        result.Should().Be(new AiConnectionTestResult(false, "OpenAI connection timed out."));
    }

    [Fact]
    public async Task TestOpenAiAsync_ShouldUseRequestTimeoutWhenProvided()
    {
        var service = new OpenAiConnectionTestService(
            new InMemoryCredentialStore(),
            new HttpClient(new DelayingHttpMessageHandler(TimeSpan.FromMilliseconds(50))),
            new OpenAiCategorizerOptions
            {
                RequestTimeout = TimeSpan.FromMilliseconds(1)
            });

        var result = await service.TestOpenAiAsync(
            new AiConnectionTestRequest("test-key", "gpt-test", TimeSpan.FromSeconds(5)),
            CancellationToken.None);

        result.Should().Be(new AiConnectionTestResult(true, "OpenAI connection succeeded."));
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

    private sealed class HttpRequestExceptionMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            throw new HttpRequestException("network unavailable");
        }
    }

    private sealed class DelayingHttpMessageHandler : HttpMessageHandler
    {
        private readonly TimeSpan delay;

        public DelayingHttpMessageHandler(TimeSpan delay)
        {
            this.delay = delay;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
