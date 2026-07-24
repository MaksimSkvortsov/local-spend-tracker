using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;

namespace Spendnest.Infrastructure.Categorization;

/// <summary>
/// Categorizes transactions with OpenAI while validating every returned category.
/// </summary>
public sealed class OpenAiTransactionCategorizer : ITransactionCategorizer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;
    private readonly OpenAiCategorizerOptions options;

    public OpenAiTransactionCategorizer(
        HttpClient httpClient,
        OpenAiCategorizerOptions options)
    {
        this.httpClient = httpClient;
        this.options = options;
    }

    public async Task<IReadOnlyList<TransactionCategorization>> CategorizeAsync(
        IReadOnlyList<Transaction> transactions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transactions);
        cancellationToken.ThrowIfCancellationRequested();

        if (transactions.Count == 0)
        {
            return [];
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException("OpenAI API key is required for OpenAI categorization.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, options.Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(CreateRequestBody(transactions), JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);

        var outputText = ReadOutputText(document.RootElement);
        using var outputDocument = JsonDocument.Parse(outputText);

        return ReadCategorizations(outputDocument.RootElement, transactions);
    }

    private object CreateRequestBody(IReadOnlyList<Transaction> transactions)
    {
        var categoryCodes = BuiltInCategories.All
            .Select(category => category.Code)
            .ToArray();

        return new
        {
            model = options.Model,
            input = new object[]
            {
                new
                {
                    role = "system",
                    content = "You categorize credit-card transactions for Spendnest. Use only the provided category codes. Return concise explanations. Credit-card payments and refunds must remain negative spending categories."
                },
                new
                {
                    role = "user",
                    content = JsonSerializer.Serialize(new
                    {
                        categories = BuiltInCategories.All.Select(category => new
                        {
                            code = category.Code,
                            name = category.Name
                        }),
                        transactions = transactions.Select(transaction => new
                        {
                            id = transaction.Id,
                            postedDate = transaction.PostedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                            amount = transaction.Amount,
                            description = transaction.OriginalDescription
                        })
                    }, JsonOptions)
                }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "spendnest_transaction_categorizations",
                    strict = true,
                    schema = new
                    {
                        type = "object",
                        additionalProperties = false,
                        properties = new
                        {
                            items = new
                            {
                                type = "array",
                                items = new
                                {
                                    type = "object",
                                    additionalProperties = false,
                                    properties = new
                                    {
                                        transactionId = new { type = "string" },
                                        categoryCode = new { type = "string", @enum = categoryCodes },
                                        confidence = new { type = "number", minimum = 0, maximum = 1 },
                                        explanation = new { type = "string" }
                                    },
                                    required = new[] { "transactionId", "categoryCode", "confidence", "explanation" }
                                }
                            }
                        },
                        required = new[] { "items" }
                    }
                }
            }
        };
    }

    private IReadOnlyList<TransactionCategorization> ReadCategorizations(
        JsonElement outputRoot,
        IReadOnlyList<Transaction> transactions)
    {
        var transactionIds = transactions.Select(transaction => transaction.Id).ToHashSet();
        var categoryCodes = BuiltInCategories.All.Select(category => category.Code).ToHashSet(StringComparer.Ordinal);

        if (!outputRoot.TryGetProperty("items", out var itemsElement) || itemsElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("OpenAI categorization response did not include an items array.");
        }

        var categorizations = new List<TransactionCategorization>();

        foreach (var item in itemsElement.EnumerateArray())
        {
            var transactionId = Guid.Parse(item.GetProperty("transactionId").GetString() ?? string.Empty);
            if (!transactionIds.Contains(transactionId))
            {
                continue;
            }

            var categoryCode = item.GetProperty("categoryCode").GetString() ?? string.Empty;
            if (!categoryCodes.Contains(categoryCode))
            {
                throw new InvalidOperationException($"OpenAI returned unsupported category code '{categoryCode}'.");
            }

            var confidence = item.GetProperty("confidence").GetDecimal();
            var explanation = item.GetProperty("explanation").GetString() ?? string.Empty;

            categorizations.Add(new TransactionCategorization(
                transactionId,
                categoryCode,
                confidence,
                confidence < options.ReviewConfidenceThreshold,
                CategorizationSource.OpenAi,
                explanation));
        }

        return categorizations;
    }

    private static string ReadOutputText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var outputTextElement))
        {
            return outputTextElement.GetString() ?? string.Empty;
        }

        if (root.TryGetProperty("output", out var outputElement))
        {
            foreach (var outputItem in outputElement.EnumerateArray())
            {
                if (!outputItem.TryGetProperty("content", out var contentElement))
                {
                    continue;
                }

                foreach (var contentItem in contentElement.EnumerateArray())
                {
                    if (contentItem.TryGetProperty("text", out var textElement))
                    {
                        return textElement.GetString() ?? string.Empty;
                    }
                }
            }
        }

        throw new InvalidOperationException("OpenAI response did not include output text.");
    }
}
