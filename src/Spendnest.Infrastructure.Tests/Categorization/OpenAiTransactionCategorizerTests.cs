namespace Spendnest.Infrastructure.Tests.Categorization;

using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Categorization;

public class OpenAiTransactionCategorizerTests
{
    [Fact]
    public async Task CategorizeAsync_ShouldSendMinimalTransactionDataAndReadStructuredOutput()
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            PostedDate = new DateOnly(2026, 7, 18),
            OriginalDescription = "BULK MART #0218 RIVERTON VA",
            Amount = 141.83m,
            SourceRowNumber = 999
        };
        string? requestBody = null;
        var categorizer = new OpenAiTransactionCategorizer(
            new HttpClient(new StubHttpMessageHandler(request =>
            {
                requestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        $$"""
                        {
                          "output_text": "{\"items\":[{\"transactionId\":\"{{transaction.Id}}\",\"categoryCode\":\"Groceries\",\"confidence\":0.91,\"explanation\":\"Warehouse club grocery purchase.\"}]}"
                        }
                        """,
                        Encoding.UTF8,
                        "application/json")
                };
            })),
            new OpenAiCategorizerOptions
            {
                ApiKey = "test-key",
                Model = "test-model"
            });

        var result = await categorizer.CategorizeAsync([transaction], CancellationToken.None);

        requestBody.Should().NotBeNull();
        using var requestJson = JsonDocument.Parse(requestBody!);
        requestJson.RootElement.GetProperty("model").GetString().Should().Be("test-model");
        var userContent = requestJson.RootElement.GetProperty("input")[1].GetProperty("content").GetString();
        using var userContentJson = JsonDocument.Parse(userContent!);
        var transactionJson = userContentJson.RootElement.GetProperty("transactions").EnumerateArray().Single();
        transactionJson.EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo(["id", "description"]);
        transactionJson.GetProperty("id").GetString().Should().Be(transaction.Id.ToString());
        transactionJson.GetProperty("description").GetString().Should().Be("BULK MART #0218 RIVERTON VA");

        result.Should().ContainSingle().Which.Should().BeEquivalentTo(new TransactionCategorization(
            transaction.Id,
            BuiltInCategoryCodes.Groceries,
            0.91m,
            false,
            CategorizationSource.OpenAi,
            "Warehouse club grocery purchase."));
    }

    [Fact]
    public async Task CategorizeAsync_ShouldMarkLowConfidenceResultsForReview()
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OriginalDescription = "MYSTERY PLACE",
            Amount = 12m
        };
        var categorizer = CreateCategorizerWithOutput(
            transaction,
            BuiltInCategoryCodes.Other,
            0.42m);

        var result = await categorizer.CategorizeAsync([transaction], CancellationToken.None);

        result.Should().ContainSingle().Which.NeedsReview.Should().BeTrue();
    }

    [Fact]
    public async Task CategorizeAsync_ShouldRejectUnsupportedCategoryCodes()
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OriginalDescription = "MYSTERY PLACE",
            Amount = 12m
        };
        var categorizer = CreateCategorizerWithOutput(
            transaction,
            "NotARealCategory",
            0.98m);

        var act = () => categorizer.CategorizeAsync([transaction], CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*unsupported category code*");
    }

    private static OpenAiTransactionCategorizer CreateCategorizerWithOutput(
        Transaction transaction,
        string categoryCode,
        decimal confidence)
    {
        return new OpenAiTransactionCategorizer(
            new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""
                    {
                      "output_text": "{\"items\":[{\"transactionId\":\"{{transaction.Id}}\",\"categoryCode\":\"{{categoryCode}}\",\"confidence\":{{confidence}},\"explanation\":\"Test explanation.\"}]}"
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            })),
            new OpenAiCategorizerOptions
            {
                ApiKey = "test-key"
            });
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
