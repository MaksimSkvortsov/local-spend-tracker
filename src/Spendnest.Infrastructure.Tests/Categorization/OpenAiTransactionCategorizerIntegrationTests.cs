namespace Spendnest.Infrastructure.Tests.Categorization;

using FluentAssertions;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Categorization;

public class OpenAiTransactionCategorizerIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CategorizeAsync_ShouldReturnCategoriesFromConfiguredOpenAi()
    {
        if (!IsEnabled())
        {
            return;
        }

        var apiKey = ReadConfiguredValue("SPENDNEST_OPENAI__APIKEY")
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        apiKey.Should().NotBeNullOrWhiteSpace("OpenAI integration tests require an API key");

        var model = ReadConfiguredValue("SPENDNEST_OPENAI__MODEL")
            ?? "gpt-5.6-luna";
        var transactions = new[]
        {
            Transaction("BULK MART #0218 RIVERTON VA", 141.83m),
            Transaction("TINY CINEMA #7781", 19.99m),
            Transaction("CAPITAL ONE MOBILE PYMT", -2193.82m)
        };
        var categorizer = new OpenAiTransactionCategorizer(
            new HttpClient(),
            new OpenAiCategorizerOptions
            {
                ApiKey = apiKey,
                Model = model,
                RequestTimeout = TimeSpan.FromSeconds(60)
            });

        var result = await categorizer.CategorizeAsync(transactions, CancellationToken.None);

        result.Should().HaveCount(transactions.Length);
        result.Select(categorization => categorization.TransactionId)
            .Should().BeEquivalentTo(transactions.Select(transaction => transaction.Id));
        result.Should().OnlyContain(categorization =>
            BuiltInCategories.All.Any(category => category.Id == categorization.CategoryId));
        result.Should().OnlyContain(categorization =>
            categorization.Confidence >= 0m && categorization.Confidence <= 1m);
        result.Should().OnlyContain(categorization => categorization.Source == CategorizationSource.Ai);
        result.Should().OnlyContain(categorization => !string.IsNullOrWhiteSpace(categorization.Explanation));
        result.Single(categorization => categorization.TransactionId == transactions[0].Id)
            .CategoryId.Should().Be(BuiltInCategoryIds.Groceries);
        result.Single(categorization => categorization.TransactionId == transactions[1].Id)
            .CategoryId.Should().Be(BuiltInCategoryIds.Entertainment);
        result.Single(categorization => categorization.TransactionId == transactions[2].Id)
            .CategoryId.Should().Be(BuiltInCategoryIds.CreditCardPayment);
    }

    private static bool IsEnabled()
    {
        return string.Equals(
            Environment.GetEnvironmentVariable("SPENDNEST_RUN_OPENAI_INTEGRATION_TESTS"),
            "1",
            StringComparison.Ordinal);
    }

    private static Transaction Transaction(
        string description,
        decimal amount)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            PostedDate = new DateOnly(2026, 7, 23),
            OriginalDescription = description,
            Amount = amount
        };
    }

    private static string? ReadConfiguredValue(string key)
    {
        var environmentValue = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            return environmentValue;
        }

        var envFilePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".env.local");
        if (!File.Exists(envFilePath))
        {
            return null;
        }

        foreach (var line in File.ReadLines(envFilePath))
        {
            var separatorIndex = line.IndexOf('=', StringComparison.Ordinal);
            if (separatorIndex <= 0)
            {
                continue;
            }

            if (!line[..separatorIndex].Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return TrimQuotes(line[(separatorIndex + 1)..].Trim());
        }

        return null;
    }

    private static string TrimQuotes(string value)
    {
        if (value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"')
                || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        return value;
    }
}
