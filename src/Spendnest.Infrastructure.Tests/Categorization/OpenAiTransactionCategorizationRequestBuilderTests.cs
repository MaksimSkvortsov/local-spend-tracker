namespace Spendnest.Infrastructure.Tests.Categorization;

using System.Text.Json;
using FluentAssertions;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Categorization;

public class OpenAiTransactionCategorizationRequestBuilderTests
{
    [Fact]
    public void Build_ShouldOmitAmountsAndIncludeTransactionDirection()
    {
        var builder = new OpenAiTransactionCategorizationRequestBuilder(new OpenAiCategorizerOptions
        {
            Model = "test-model"
        });
        var charge = Transaction("BULK MART #0218 RIVERTON VA", 141.83m);
        var refund = Transaction("TARGET REFUND", -42.19m);
        var requestJson = JsonSerializer.Serialize(
            builder.Build([charge, refund]),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        using var requestDocument = JsonDocument.Parse(requestJson);
        requestDocument.RootElement.GetProperty("model").GetString().Should().Be("test-model");
        var userContent = requestDocument.RootElement.GetProperty("input")[1].GetProperty("content").GetString();
        using var userContentDocument = JsonDocument.Parse(userContent!);
        var transactions = userContentDocument.RootElement.GetProperty("transactions").EnumerateArray().ToArray();

        transactions[0].GetProperty("transactionDirection").GetString().Should().Be("charge");
        transactions[1].GetProperty("transactionDirection").GetString().Should().Be("refund_or_credit");
        foreach (var transaction in transactions)
        {
            transaction.TryGetProperty("amount", out _).Should().BeFalse();
        }
    }

    private static Transaction Transaction(
        string description,
        decimal amount)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            OriginalDescription = description,
            Amount = amount,
            PostedDate = new DateOnly(2026, 7, 24)
        };
    }
}
