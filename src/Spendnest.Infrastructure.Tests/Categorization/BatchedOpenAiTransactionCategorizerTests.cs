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
                var batchTransactionIds = ReadTransactionIdsFromRequest(request);
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
    public async Task CategorizeAsync_ShouldClampInvalidBatchSizeToOne()
    {
        var transactions = new[]
        {
            Transaction("FIRST PLACE"),
            Transaction("SECOND PLACE")
        };
        var requestTransactionCounts = new List<int>();
        var categorizer = new BatchedOpenAiTransactionCategorizer(
            new InMemoryCredentialStore(new Dictionary<string, string?>
            {
                [CredentialKeys.OpenAiApiKey] = "test-key"
            }),
            new HttpClient(new StubHttpMessageHandler(request =>
            {
                var batchTransactionIds = ReadTransactionIdsFromRequest(request);
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
                MaxTransactionsPerRequest = 0
            });

        var result = await categorizer.CategorizeAsync(transactions, CancellationToken.None);

        requestTransactionCounts.Should().Equal(1, 1);
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
                if (requestCount >= 2)
                {
                    throw new TimeoutException("Batch timeout.");
                }

                var batchTransactionIds = ReadTransactionIdsFromRequest(request);
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
        requestCount.Should().Be(3, "the timed-out batch is retried once");
    }

    [Fact]
    public async Task CategorizeAsync_ShouldRetryTimedOutBatchOnceAsTwoHalves()
    {
        var transactions = Enumerable.Range(1, 5)
            .Select(index => Transaction($"PLACE {index}"))
            .ToArray();
        var requestSizes = new List<int>();
        var categorizer = CreateCategorizer(
            batchTransactionIds =>
            {
                requestSizes.Add(batchTransactionIds.Length);
                return requestSizes.Count == 1;
            },
            maxTransactionsPerRequest: 5);

        var result = await categorizer.CategorizeAsync(transactions, CancellationToken.None);

        requestSizes.Should().Equal(5, 3, 2);
        result.Should().HaveCount(5).And.OnlyContain(categorization => categorization.Source == CategorizationSource.Ai);
    }

    [Fact]
    public async Task CategorizeAsync_ShouldReturnUnresolvedOnlyForHalfThatTimesOutAgain()
    {
        var transactions = Enumerable.Range(1, 4)
            .Select(index => Transaction($"PLACE {index}"))
            .ToArray();
        var requestCount = 0;
        var categorizer = CreateCategorizer(
            _ =>
            {
                requestCount++;
                return requestCount is 1 or 3;
            },
            maxTransactionsPerRequest: 4);

        var result = await categorizer.CategorizeAsync(transactions, CancellationToken.None);

        requestCount.Should().Be(3);
        result.Where(categorization => categorization.Source == CategorizationSource.Ai)
            .Select(categorization => categorization.TransactionId)
            .Should().Equal(transactions[0].Id, transactions[1].Id);
        result.Where(categorization => categorization.Source == CategorizationSource.Unresolved)
            .Select(categorization => categorization.TransactionId)
            .Should().Equal(transactions[2].Id, transactions[3].Id);
    }

    [Fact]
    public async Task CategorizeAsync_ShouldResendTransactionsMissingFromResponseOnce()
    {
        var transactions = Enumerable.Range(1, 3)
            .Select(index => Transaction($"PLACE {index}"))
            .ToArray();
        var requests = new List<string[]>();
        var categorizer = CreateCategorizer(
            _ => false,
            maxTransactionsPerRequest: 3,
            answeredIds: batchTransactionIds =>
            {
                requests.Add(batchTransactionIds);
                return requests.Count == 1 ? batchTransactionIds.Skip(1) : batchTransactionIds;
            });

        var result = await categorizer.CategorizeAsync(transactions, CancellationToken.None);

        requests.Should().HaveCount(2);
        requests[1].Should().Equal(transactions[0].Id.ToString());
        result.Select(categorization => categorization.TransactionId)
            .Should().BeEquivalentTo(transactions.Select(transaction => transaction.Id));
        result.Should().OnlyContain(categorization => categorization.Source == CategorizationSource.Ai);
    }

    [Fact]
    public async Task CategorizeAsync_ShouldLeaveTransactionWithoutResultWhenRetryAlsoOmitsIt()
    {
        var transactions = Enumerable.Range(1, 3)
            .Select(index => Transaction($"PLACE {index}"))
            .ToArray();
        var skippedId = transactions[1].Id.ToString();
        var requestCount = 0;
        var categorizer = CreateCategorizer(
            _ => false,
            maxTransactionsPerRequest: 3,
            answeredIds: batchTransactionIds =>
            {
                requestCount++;
                return batchTransactionIds.Where(id => id != skippedId);
            });

        var result = await categorizer.CategorizeAsync(transactions, CancellationToken.None);

        requestCount.Should().Be(2);
        result.Select(categorization => categorization.TransactionId)
            .Should().BeEquivalentTo([transactions[0].Id, transactions[2].Id]);
    }

    /// <summary>
    /// Creates a categorizer whose fake OpenAI endpoint times out when <paramref name="timesOut"/> returns true.
    /// </summary>
    private static BatchedOpenAiTransactionCategorizer CreateCategorizer(
        Func<string[], bool> timesOut,
        int maxTransactionsPerRequest,
        Func<string[], IEnumerable<string>>? answeredIds = null)
    {
        return new BatchedOpenAiTransactionCategorizer(
            new InMemoryCredentialStore(new Dictionary<string, string?>
            {
                [CredentialKeys.OpenAiApiKey] = "test-key"
            }),
            new HttpClient(new StubHttpMessageHandler(request =>
            {
                var batchTransactionIds = ReadTransactionIdsFromRequest(request);
                if (timesOut(batchTransactionIds))
                {
                    throw new TimeoutException("Batch timeout.");
                }

                var responseItems = (answeredIds?.Invoke(batchTransactionIds) ?? batchTransactionIds).Select(transactionId => new
                {
                    transactionId,
                    categoryId = BuiltInCategoryIds.Groceries,
                    rulePrefix = "PLACE",
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
                MaxTransactionsPerRequest = maxTransactionsPerRequest
            });
    }

    [Fact]
    public async Task CategorizeAsync_ShouldPropagateCallerCancellation()
    {
        var transaction = Transaction("FIRST PLACE");
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var categorizer = new BatchedOpenAiTransactionCategorizer(
            new InMemoryCredentialStore(new Dictionary<string, string?>
            {
                [CredentialKeys.OpenAiApiKey] = "test-key"
            }),
            new HttpClient(new ThrowingHttpMessageHandler()),
            new OpenAiCategorizerOptions());

        var act = () => categorizer.CategorizeAsync([transaction], cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static string[] ReadTransactionIdsFromRequest(HttpRequestMessage request)
    {
        var requestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
        using var requestJson = JsonDocument.Parse(requestBody);
        var userContent = requestJson.RootElement.GetProperty("input")[1].GetProperty("content").GetString();
        using var userContentJson = JsonDocument.Parse(userContent!);

        return userContentJson.RootElement
            .GetProperty("transactions")
            .EnumerateArray()
            .Select(transaction => transaction.GetProperty("id").GetString()!)
            .ToArray();
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
