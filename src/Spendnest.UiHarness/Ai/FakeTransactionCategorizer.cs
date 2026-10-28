using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Credentials;
using Spendnest.Core.Transactions;
using Spendnest.UiHarness.Seeding;

namespace Spendnest.UiHarness.Ai;

/// <summary>
/// Stands in for OpenAI categorization in browser tests. Like the real categorizer it needs a saved API key;
/// it then matches seed catalog merchants by keyword and sends everything else to review as Other.
/// </summary>
public sealed class FakeTransactionCategorizer(ICredentialStore credentialStore) : ITransactionCategorizer
{
    public const decimal MatchedConfidence = 0.92m;

    public const decimal UnmatchedConfidence = 0.45m;

    private static readonly IReadOnlyList<SeedTransactionTemplate> KnownMerchants =
        SeedCardCatalog.All.SelectMany(card => card.MonthlyTransactions).ToArray();

    public async Task<IReadOnlyList<TransactionCategorization>> CategorizeAsync(
        IReadOnlyList<Transaction> transactions,
        CancellationToken cancellationToken)
    {
        var apiKey = await credentialStore.GetStringAsync(CredentialKeys.OpenAiApiKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new AiCategorizationUnavailableException("OpenAI API key is required for OpenAI categorization.");
        }

        return transactions.Select(Categorize).ToArray();
    }

    private static TransactionCategorization Categorize(Transaction transaction)
    {
        var match = KnownMerchants.FirstOrDefault(template =>
            transaction.OriginalDescription.Contains(template.Merchant, StringComparison.OrdinalIgnoreCase));

        return match is null || match.NeedsReview
            ? new TransactionCategorization(
                transaction.Id,
                BuiltInCategoryIds.Other,
                UnmatchedConfidence,
                NeedsReview: true,
                CategorizationSource.Ai,
                "Fake AI could not identify the merchant.")
            : new TransactionCategorization(
                transaction.Id,
                match.CategoryId,
                MatchedConfidence,
                NeedsReview: false,
                CategorizationSource.Ai,
                $"Fake AI matched '{match.Merchant}'.");
    }
}
