namespace Spendnest.Infrastructure.Tests.Categorization;

using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Credentials;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Categorization;
using Spendnest.Infrastructure.Credentials;

public class BatchedOpenAiTransactionCategorizerTests
{
    [Fact]
    public async Task CategorizeAsync_ShouldRequireApiKey()
    {
        var categorizer = new BatchedOpenAiTransactionCategorizer(
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
        var categorizer = new BatchedOpenAiTransactionCategorizer(
            new InMemoryCredentialStore(new Dictionary<string, string?>
            {
                [CredentialKeys.OpenAiApiKey] = "test-key"
            }),
            new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""
                    {
                      "output_text": "{\"items\":[{\"transactionId\":\"{{transaction.Id}}\",\"categoryId\":{{BuiltInCategoryIds.Other}},\"rulePrefix\":\"MYSTERY PLACE\",\"confidence\":0.88,\"explanation\":\"Test AI result.\"}]}"
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            })),
            new OpenAiCategorizerOptions());

        var result = await categorizer.CategorizeAsync([transaction], CancellationToken.None);

        result.Should().ContainSingle().Which.Should().BeEquivalentTo(new TransactionCategorization(
            transaction.Id,
            BuiltInCategoryIds.Other,
            0.88m,
            false,
            CategorizationSource.Ai,
            "Test AI result.",
            "MYSTERY PLACE"));
    }

    [Fact]
    public async Task CategorizeAsync_ShouldSplitLargeInputsIntoConfiguredBatches()
    {
        var transactions = new[]
        {
            Transaction("FIRST PLACE"),
            Transaction("SECOND PLACE"),
            Transaction("THIRD PLACE")
        };
        var requestTransactionCounts = new List<int>();
        var categorizer = new BatchedOpenAiTransactionCategorizer(
            new InMemoryCredentialStore(new Dictionary<string, string?>
            {
                [CredentialKeys.OpenAiApiKey] = "test-key"
            }),
            new HttpClient(new StubHttpMessageHandler(request =>
            {
                var requestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                using var requestJson = JsonDocument.Parse(requestBody);
                var userContent = requestJson.RootElement.GetProperty("input")[1].GetProperty("content").GetString();
                using var userContentJson = JsonDocument.Parse(userContent!);
                var batchTransactionIds = userContentJson.RootElement
                    .GetProperty("transactions")
                    .EnumerateArray()
                    .Select(transaction => transaction.GetProperty("id").GetString())
                    .ToArray();
                requestTransactionCounts.Add(batchTransactionIds.Length);
                var responseItems = batchTransactionIds.Select(transactionId => new
                {
                    transactionId,
                    categoryId = BuiltInCategoryIds.Other,
                    rulePrefix = "TEST PLACE",
                    confidence = 0.82m,
                    explanation = "Test batch result."
                });
                var output = JsonSerializer.Serialize(new { items = responseItems });

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { output_text = output }),
                        Encoding.UTF8,
                        "application/json")
                };
            })),
            new OpenAiCategorizerOptions
            {
                MaxTransactionsPerRequest = 2
            });

        var result = await categorizer.CategorizeAsync(transactions, CancellationToken.None);

        requestTransactionCounts.Should().Equal(2, 1);
        result.Select(categorization => categorization.TransactionId)
            .Should().BeEquivalentTo(transactions.Select(transaction => transaction.Id));
    }

    [Fact]
    public async Task CategorizeAsync_ShouldKeepSuccessfulBatchesWhenLaterBatchTimesOut()
    {
        var transactions = new[]
        {
            Transaction("FIRST PLACE"),
            Transaction("SECOND PLACE"),
            Transaction("THIRD PLACE")
        };
        var requestCount = 0;
        var categorizer = new BatchedOpenAiTransactionCategorizer(
            new InMemoryCredentialStore(new Dictionary<string, string?>
            {
                [CredentialKeys.OpenAiApiKey] = "test-key"
            }),
            new HttpClient(new StubHttpMessageHandler(request =>
            {
                requestCount++;
                if (requestCount == 2)
                {
                    throw new TimeoutException("Batch timeout.");
                }

                var requestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                using var requestJson = JsonDocument.Parse(requestBody);
                var userContent = requestJson.RootElement.GetProperty("input")[1].GetProperty("content").GetString();
                using var userContentJson = JsonDocument.Parse(userContent!);
                var batchTransactionIds = userContentJson.RootElement
                    .GetProperty("transactions")
                    .EnumerateArray()
                    .Select(transaction => transaction.GetProperty("id").GetString())
                    .ToArray();
                var responseItems = batchTransactionIds.Select(transactionId => new
                {
                    transactionId,
                    categoryId = BuiltInCategoryIds.Groceries,
                    rulePrefix = "TEST PLACE",
                    confidence = 0.91m,
                    explanation = "Test batch result."
                });
                var output = JsonSerializer.Serialize(new { items = responseItems });

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { output_text = output }),
                        Encoding.UTF8,
                        "application/json")
                };
            })),
            new OpenAiCategorizerOptions
            {
                MaxTransactionsPerRequest = 2
            });

        var result = await categorizer.CategorizeAsync(transactions, CancellationToken.None);

        result.Should().HaveCount(3);
        result.Where(categorization => categorization.Source == CategorizationSource.Ai)
            .Should().HaveCount(2);
        result.Should().ContainSingle(categorization =>
            categorization.TransactionId == transactions[2].Id
            && categorization.Source == CategorizationSource.Unresolved
            && categorization.Explanation == "AI categorization timed out.");
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
