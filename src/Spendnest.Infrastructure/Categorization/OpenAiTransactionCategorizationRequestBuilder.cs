using System.Text.Json;
using Spendnest.Core.Categories;
using Spendnest.Core.Transactions;

namespace Spendnest.Infrastructure.Categorization;

public sealed class OpenAiTransactionCategorizationRequestBuilder
{
    private readonly OpenAiCategorizerOptions options;

    public OpenAiTransactionCategorizationRequestBuilder(OpenAiCategorizerOptions options)
    {
        this.options = options;
    }

    public object Build(IReadOnlyList<Transaction> transactions)
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
                            guidance = OpenAiCategoryGuidance.Get(category.Id)
                        }),
                        transactions = transactions.Select(transaction => new
                        {
                            id = transaction.Id,
                            description = transaction.OriginalDescription,
                            transactionDirection = GetTransactionDirection(transaction)
                        })
                    }, OpenAiJson.Options)
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

}
