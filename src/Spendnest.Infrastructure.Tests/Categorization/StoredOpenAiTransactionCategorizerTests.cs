namespace Spendnest.Infrastructure.Tests.Categorization;

using System.Net;
using System.Text;
using FluentAssertions;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Credentials;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Categorization;
using Spendnest.Infrastructure.Credentials;

public class StoredOpenAiTransactionCategorizerTests
{
    [Fact]
    public async Task CategorizeAsync_ShouldRequireApiKey()
    {
        var categorizer = new StoredOpenAiTransactionCategorizer(
            new InMemoryCredentialStore(),
            new HttpClient(new ThrowingHttpMessageHandler()),
            new OpenAiCategorizerOptions());
        var transaction = Transaction("BULK MART #0218 RIVERTON VA");

        var act = () => categorizer.CategorizeAsync([transaction], CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("OpenAI API key is required*");
    }

    [Fact]
    public async Task CategorizeAsync_ShouldUseOpenAiWhenApiKeyIsSaved()
    {
        var transaction = Transaction("MYSTERY PLACE");
        var categorizer = new StoredOpenAiTransactionCategorizer(
            new InMemoryCredentialStore(new Dictionary<string, string?>
            {
                [CredentialKeys.OpenAiApiKey] = "test-key"
            }),
            new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""
                    {
                      "output_text": "{\"items\":[{\"transactionId\":\"{{transaction.Id}}\",\"categoryCode\":\"Other\",\"confidence\":0.88,\"explanation\":\"Test AI result.\"}]}"
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            })),
            new OpenAiCategorizerOptions());

        var result = await categorizer.CategorizeAsync([transaction], CancellationToken.None);

        result.Should().ContainSingle().Which.Should().BeEquivalentTo(new TransactionCategorization(
            transaction.Id,
            BuiltInCategoryCodes.Other,
            0.88m,
            false,
            CategorizationSource.OpenAi,
            "Test AI result."));
    }

    private static Transaction Transaction(string description)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            OriginalDescription = description,
            Amount = 10m,
            PostedDate = new DateOnly(2026, 7, 24)
        };
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
