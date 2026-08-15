namespace Spendnest.Infrastructure.Tests.Categorization;

using System.Text.Json;
using FluentAssertions;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Categorization;

public class OpenAiTransactionCategorizationResponseReaderTests
{
    [Fact]
    public void Read_ShouldReadKnownTransactionResults()
    {
        var transaction = Transaction("BULK MART #0218 RIVERTON VA");
        var reader = CreateReader();
        var outputText = OutputText(new
        {
            items = new[]
            {
                new
                {
                    transactionId = transaction.Id,
                    categoryId = BuiltInCategoryIds.Groceries,
                    rulePrefix = "BULK MART",
                    confidence = 0.91m,
                    explanation = "Warehouse club grocery purchase."
                }
            }
        });

        var result = reader.Read(outputText, [transaction]);

        result.Should().ContainSingle().Which.Should().BeEquivalentTo(new TransactionCategorization(
            transaction.Id,
            BuiltInCategoryIds.Groceries,
            0.91m,
            false,
            CategorizationSource.Ai,
            "Warehouse club grocery purchase.",
            "BULK MART"));
    }

    [Fact]
    public void Read_ShouldIgnoreResultsForUnknownTransactions()
    {
        var transaction = Transaction("BULK MART #0218 RIVERTON VA");
        var reader = CreateReader();
        var outputText = OutputText(new
        {
            items = new[]
            {
                new
                {
                    transactionId = Guid.NewGuid(),
                    categoryId = BuiltInCategoryIds.Groceries,
                    rulePrefix = "BULK MART",
                    confidence = 0.91m,
                    explanation = "Unknown transaction."
                }
            }
        });

        var result = reader.Read(outputText, [transaction]);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Read_ShouldRejectMissingItemsArray()
    {
        var transaction = Transaction("BULK MART #0218 RIVERTON VA");
        var reader = CreateReader();
        var outputText = OutputText(new { results = Array.Empty<object>() });

        var act = () => reader.Read(outputText, [transaction]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("OpenAI categorization response did not include an items array.");
    }

    private static OpenAiTransactionCategorizationResponseReader CreateReader()
    {
        return new OpenAiTransactionCategorizationResponseReader(new OpenAiCategorizerOptions());
    }

    private static string OutputText(object output)
    {
        return JsonSerializer.Serialize(output);
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
}
