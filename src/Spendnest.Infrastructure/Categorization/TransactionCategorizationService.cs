using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Progress;
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
    private readonly ICategoryRuleRepository categoryRuleRepository;
    private readonly ITransactionMerchantCodeResolver merchantCodeResolver;

    public TransactionCategorizationService(
        ILocalTransactionCategorizer localCategorizer,
        ITransactionCategorizer aiCategorizer,
        ITransactionCategoryAssignmentRepository assignmentRepository,
        ICategoryRuleRepository categoryRuleRepository,
        ITransactionMerchantCodeResolver merchantCodeResolver)
    {
        this.localCategorizer = localCategorizer;
        this.aiCategorizer = aiCategorizer;
        this.assignmentRepository = assignmentRepository;
        this.categoryRuleRepository = categoryRuleRepository;
        this.merchantCodeResolver = merchantCodeResolver;
    }

    public async Task<IReadOnlyList<TransactionCategorization>> CategorizeAsync(
        IReadOnlyList<Transaction> transactions,
        CancellationToken cancellationToken)
    {
        return await CategorizeAsync(transactions, null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TransactionCategorization>> CategorizeAsync(
        IReadOnlyList<Transaction> transactions,
        IProgress<FileUploadProgress>? progress,
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
            progress?.Report(new FileUploadProgress(
                FileUploadProgressStage.CategorizingWithAi,
                "Categorizing with AI",
                0,
                unresolvedTransactions.Length));
            aiResults = await aiCategorizer.CategorizeAsync(unresolvedTransactions, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            aiResults = unresolvedTransactions
                .Select(transaction => CreateUnresolvedResult(transaction, "AI categorization timed out."))
                .ToArray();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
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
        await RememberAcceptedAiRulesAsync(
            acceptedAiResults,
            unresolvedTransactions,
            cancellationToken).ConfigureAwait(false);
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

    private async Task RememberAcceptedAiRulesAsync(
        IReadOnlyList<TransactionCategorization> acceptedAiResults,
        IReadOnlyList<Transaction> unresolvedTransactions,
        CancellationToken cancellationToken)
    {
        var transactionsById = unresolvedTransactions.ToDictionary(transaction => transaction.Id);

        foreach (var result in acceptedAiResults)
        {
            if (result.NeedsReview
                || result.Source is CategorizationSource.Unresolved
                || result.CategoryId == BuiltInCategoryIds.Other
                || !transactionsById.TryGetValue(result.TransactionId, out var transaction))
            {
                continue;
            }

            await categoryRuleRepository.AddAsync(
                new CategoryRule
                {
                    Pattern = merchantCodeResolver.Resolve(transaction),
                    CategoryId = result.CategoryId,
                    MatchType = CategoryRuleMatchType.Exact
                },
                cancellationToken).ConfigureAwait(false);
        }
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
