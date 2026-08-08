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
        var requestJson = JsonSerializer.Serialize(CreateRequestBody(transactions), JsonOptions);
        request.Content = new StringContent(
            requestJson,
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
        var categoryIds = BuiltInCategories.All
            .Select(category => category.Id)
            .ToArray();

        return new
        {
            model = options.Model,
            input = new object[]
            {
                new
                {
                    role = "system",
                    content = "You categorize credit-card transactions for Spendnest. Use only the provided category ids. Categorize using the full transaction description. Also return rulePrefix: the shortest stable uppercase merchant prefix this app can store and use with starts-with matching on future imports. Remove store numbers, dates, locations, authorization numbers, and other noisy suffixes, but keep enough words to avoid mixing distinct services such as AMAZON, AMAZON PRIME, and AMAZON WEB SERVICES. Choose the best-fit category for common merchants; do not overuse Other. Use Other only when the merchant or transaction type cannot reasonably fit a provided category. Exact amounts are intentionally omitted for privacy. Refunds and credits are not a category. If transactionDirection is refund_or_credit, use the original spending category when it can be inferred. Credit-card payments should use Credit Card Payment. Return concise explanations."
                },
                new
                {
                    role = "user",
                    content = JsonSerializer.Serialize(new
                    {
                        categories = BuiltInCategories.All.Select(category => new
                        {
                            id = category.Id,
                            name = category.Name,
                            guidance = GetCategoryGuidance(category.Id)
                        }),
                        transactions = transactions.Select(transaction => new
                        {
                            id = transaction.Id,
                            description = transaction.OriginalDescription,
                            transactionDirection = GetTransactionDirection(transaction)
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
                                        categoryId = new { type = "integer", @enum = categoryIds },
                                        rulePrefix = new { type = "string" },
                                        confidence = new { type = "number", minimum = 0, maximum = 1 },
                                        explanation = new { type = "string" }
                                    },
                                    required = new[] { "transactionId", "categoryId", "rulePrefix", "confidence", "explanation" }
                                }
                            }
                        },
                        required = new[] { "items" }
                    }
                }
            }
        };
    }

    private static string GetTransactionDirection(Transaction transaction)
    {
        return transaction.Amount < 0
            ? "refund_or_credit"
            : "charge";
    }

    private static string GetCategoryGuidance(int categoryId)
    {
        return categoryId switch
        {
            BuiltInCategoryIds.Groceries => "Grocery stores, supermarkets, warehouse groceries, food markets, Costco/Trader Joe's/Giant/Wegmans/Harris Teeter/Food Lion/Whole Foods.",
            BuiltInCategoryIds.RestaurantsAndCoffee => "Restaurants, cafes, bakeries, bars, delivery services, DoorDash, Grubhub, Potbelly, pizza, sushi, coffee shops.",
            BuiltInCategoryIds.Transportation => "Gas, rideshare, taxis, parking, tolls, car service, Uber, Lyft, fuel stations.",
            BuiltInCategoryIds.Shopping => "Retail stores, Amazon purchases, Target, Home Depot, clothing, electronics, household goods.",
            BuiltInCategoryIds.Entertainment => "Concerts, movies, museums, parks, tickets, clubs, zoo, shows.",
            BuiltInCategoryIds.Travel => "Hotels, airfare, Airbnb, booking sites, rental cars, travel insurance, tourist transport.",
            BuiltInCategoryIds.Healthcare => "Doctors, pharmacies, medical, dental, vision, health services.",
            BuiltInCategoryIds.Utilities => "Water, electric, gas utilities, internet, phone, municipal services.",
            BuiltInCategoryIds.Subscriptions => "Recurring digital services, memberships, streaming, software subscriptions.",
            BuiltInCategoryIds.Insurance => "Insurance premiums and insurance providers.",
            BuiltInCategoryIds.PersonalCare => "Haircuts, barber, spa, massage, cosmetics, grooming.",
            BuiltInCategoryIds.FeesAndCharges => "Interest charges, bank fees, card fees, service charges.",
            BuiltInCategoryIds.CreditCardPayment => "Credit-card payments, statement payments, Capital One mobile payments.",
            BuiltInCategoryIds.Other => "Only for transactions that cannot reasonably fit another provided category.",
            _ => string.Empty
        };
    }

    private IReadOnlyList<TransactionCategorization> ReadCategorizations(
        JsonElement outputRoot,
        IReadOnlyList<Transaction> transactions)
    {
        var transactionIds = transactions.Select(transaction => transaction.Id).ToHashSet();
        var categoryIds = BuiltInCategories.All.Select(category => category.Id).ToHashSet();

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

            var categoryId = item.GetProperty("categoryId").GetInt32();
            if (!categoryIds.Contains(categoryId))
            {
                throw new InvalidOperationException($"OpenAI returned unsupported category id '{categoryId}'.");
            }

            var rulePrefix = item.GetProperty("rulePrefix").GetString() ?? string.Empty;
            var confidence = item.GetProperty("confidence").GetDecimal();
            var explanation = item.GetProperty("explanation").GetString() ?? string.Empty;

            categorizations.Add(new TransactionCategorization(
                transactionId,
                categoryId,
                confidence,
                confidence < options.ReviewConfidenceThreshold,
                CategorizationSource.Ai,
                explanation,
                rulePrefix));
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
