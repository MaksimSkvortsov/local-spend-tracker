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
    private readonly ITransactionCategoryAssignmentRepository assignmentRepository;

    public TransactionCategorizationService(
        ILocalTransactionCategorizer localCategorizer,
        ITransactionCategorizer aiCategorizer,
        ITransactionCategoryAssignmentRepository assignmentRepository)
    {
        this.localCategorizer = localCategorizer;
        this.aiCategorizer = aiCategorizer;
        this.assignmentRepository = assignmentRepository;
    }

    public async Task<IReadOnlyList<TransactionCategorization>> CategorizeAsync(
        IReadOnlyList<Transaction> transactions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transactions);
        cancellationToken.ThrowIfCancellationRequested();

        var storedAssignments = await assignmentRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        var transactionIds = transactions.Select(transaction => transaction.Id).ToHashSet();
        var existingAssignments = storedAssignments
            .Where(assignment => transactionIds.Contains(assignment.TransactionId) && !assignment.NeedsReview)
            .Select(assignment => new TransactionCategorization(
                assignment.TransactionId,
                assignment.CategoryId,
                assignment.Confidence,
                false,
                assignment.Source,
                assignment.Explanation))
            .ToArray();
        var transactionIdsWithExistingAssignments = existingAssignments
            .Select(assignment => assignment.TransactionId)
            .ToHashSet();
        var transactionsToCategorize = transactions
            .Where(transaction => !transactionIdsWithExistingAssignments.Contains(transaction.Id))
            .ToArray();

        var localResults = await localCategorizer.CategorizeKnownAsync(transactionsToCategorize, cancellationToken).ConfigureAwait(false);
        var categorizedTransactionIds = existingAssignments
            .Concat(localResults)
            .Select(result => result.TransactionId)
            .ToHashSet();
        var unresolvedTransactions = transactions
            .Where(transaction => !categorizedTransactionIds.Contains(transaction.Id))
            .ToArray();

        if (unresolvedTransactions.Length == 0)
        {
            return existingAssignments.Concat(localResults).ToArray();
        }

        IReadOnlyList<TransactionCategorization> aiResults;
        try
        {
            aiResults = await aiCategorizer.CategorizeAsync(unresolvedTransactions, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            aiResults = unresolvedTransactions
                .Select(transaction => CreateUnresolvedResult(transaction, "AI categorization failed."))
                .ToArray();
        }

        var validCategoryIds = BuiltInCategories.All.Select(category => category.Id).ToHashSet();
        var validTransactionIds = unresolvedTransactions.Select(transaction => transaction.Id).ToHashSet();
        var acceptedAiResults = aiResults
            .Where(result => validTransactionIds.Contains(result.TransactionId))
            .Select(result => validCategoryIds.Contains(result.CategoryId)
                ? result
                : CreateInvalidResult(result))
            .ToArray();
        var aiResultIds = acceptedAiResults.Select(result => result.TransactionId).ToHashSet();
        var stillUnresolved = unresolvedTransactions
            .Where(transaction => !aiResultIds.Contains(transaction.Id))
            .Select(transaction => CreateUnresolvedResult(transaction, "No category result was returned."))
            .ToArray();

        return existingAssignments
            .Concat(localResults)
            .Concat(acceptedAiResults)
            .Concat(stillUnresolved)
            .ToArray();
    }

    private static TransactionCategorization CreateInvalidResult(TransactionCategorization result)
    {
        return result with
        {
            CategoryId = BuiltInCategoryIds.Other,
            Confidence = 0m,
            NeedsReview = true,
            Source = CategorizationSource.Unresolved,
            Explanation = $"Rejected unsupported category id '{result.CategoryId}'."
        };
    }

    private static TransactionCategorization CreateUnresolvedResult(
        Transaction transaction,
        string explanation)
    {
        return new TransactionCategorization(
            transaction.Id,
            BuiltInCategoryIds.Other,
            0m,
            true,
            CategorizationSource.Unresolved,
            explanation);
    }
}
