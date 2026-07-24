using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;

namespace Spendnest.Infrastructure.Categorization;

/// <summary>
/// Runs local categorization first and uses AI only for remaining transactions.
/// </summary>
public sealed class TransactionCategorizationService : ITransactionCategorizationService
{
    private readonly ILocalTransactionCategorizer localCategorizer;
    private readonly ITransactionCategorizer aiCategorizer;

    public TransactionCategorizationService(
        ILocalTransactionCategorizer localCategorizer,
        ITransactionCategorizer aiCategorizer)
    {
        this.localCategorizer = localCategorizer;
        this.aiCategorizer = aiCategorizer;
    }

    public async Task<IReadOnlyList<TransactionCategorization>> CategorizeAsync(
        IReadOnlyList<Transaction> transactions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transactions);
        cancellationToken.ThrowIfCancellationRequested();

        var localDecisions = await localCategorizer.CategorizeKnownAsync(transactions, cancellationToken).ConfigureAwait(false);
        var categorizedTransactionIds = localDecisions.Select(decision => decision.TransactionId).ToHashSet();
        var unresolvedTransactions = transactions
            .Where(transaction => !categorizedTransactionIds.Contains(transaction.Id))
            .ToArray();

        if (unresolvedTransactions.Length == 0)
        {
            return localDecisions;
        }

        IReadOnlyList<TransactionCategorization> aiDecisions;
        try
        {
            aiDecisions = await aiCategorizer.CategorizeAsync(unresolvedTransactions, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            aiDecisions = unresolvedTransactions
                .Select(transaction => CreateUnresolvedDecision(transaction, "AI categorization failed."))
                .ToArray();
        }

        var validCategoryCodes = BuiltInCategories.All.Select(category => category.Code).ToHashSet(StringComparer.Ordinal);
        var validTransactionIds = unresolvedTransactions.Select(transaction => transaction.Id).ToHashSet();
        var acceptedAiDecisions = aiDecisions
            .Where(decision => validTransactionIds.Contains(decision.TransactionId))
            .Select(decision => validCategoryCodes.Contains(decision.CategoryCode)
                ? decision
                : CreateInvalidDecision(decision))
            .ToArray();
        var aiDecisionIds = acceptedAiDecisions.Select(decision => decision.TransactionId).ToHashSet();
        var stillUnresolved = unresolvedTransactions
            .Where(transaction => !aiDecisionIds.Contains(transaction.Id))
            .Select(transaction => CreateUnresolvedDecision(transaction, "No category decision was returned."))
            .ToArray();

        return localDecisions
            .Concat(acceptedAiDecisions)
            .Concat(stillUnresolved)
            .ToArray();
    }

    private static TransactionCategorization CreateInvalidDecision(TransactionCategorization decision)
    {
        return decision with
        {
            CategoryCode = BuiltInCategoryCodes.Other,
            Confidence = 0m,
            NeedsReview = true,
            Source = CategorizationSource.Unresolved,
            Explanation = $"Rejected unsupported category code '{decision.CategoryCode}'."
        };
    }

    private static TransactionCategorization CreateUnresolvedDecision(
        Transaction transaction,
        string explanation)
    {
        return new TransactionCategorization(
            transaction.Id,
            BuiltInCategoryCodes.Other,
            0m,
            true,
            CategorizationSource.Unresolved,
            explanation);
    }
}
