using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Progress;
using Spendnest.Core.Transactions;

namespace Spendnest.Application.Categorization;

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
    private readonly ILogger<TransactionCategorizationService> logger;

    public TransactionCategorizationService(
        ILocalTransactionCategorizer localCategorizer,
        ITransactionCategorizer aiCategorizer,
        ITransactionCategoryAssignmentRepository assignmentRepository,
        ICategoryRuleRepository categoryRuleRepository,
        ITransactionMerchantCodeResolver merchantCodeResolver,
        ILogger<TransactionCategorizationService>? logger = null)
    {
        this.localCategorizer = localCategorizer;
        this.aiCategorizer = aiCategorizer;
        this.assignmentRepository = assignmentRepository;
        this.categoryRuleRepository = categoryRuleRepository;
        this.merchantCodeResolver = merchantCodeResolver;
        this.logger = logger ?? NullLogger<TransactionCategorizationService>.Instance;
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
        logger.LogInformation(
            "Categorization prepared {TransactionCount} transactions: {ExistingCount} existing, {LocalCount} local, {UnresolvedCount} unresolved.",
            transactions.Count,
            existingAssignments.Length,
            localResults.Count,
            unresolvedTransactions.Length);

        if (unresolvedTransactions.Length == 0)
        {
            return existingAssignments.Concat(localResults).ToArray();
        }

        var unresolvedGroups = unresolvedTransactions
            .GroupBy(transaction => merchantCodeResolver.Resolve(transaction), StringComparer.Ordinal)
            .ToArray();
        var representativeTransactions = unresolvedGroups
            .Select(group => group.First())
            .ToArray();

        IReadOnlyList<TransactionCategorization> aiResults;
        try
        {
            logger.LogInformation(
                "Sending {RepresentativeCount} representative transactions to AI for {UnresolvedCount} unresolved transactions.",
                representativeTransactions.Length,
                unresolvedTransactions.Length);
            progress?.Report(FileUploadProgress.CategorizingWithAi(
                0,
                representativeTransactions.Length));
            var representativeAiResults = await aiCategorizer.CategorizeAsync(representativeTransactions, cancellationToken).ConfigureAwait(false);
            aiResults = CreateAiResultsForUnresolvedTransactions(
                representativeAiResults,
                representativeTransactions,
                unresolvedTransactions);
            logger.LogInformation(
                "AI categorization returned {AiResultCount} expanded results for {UnresolvedCount} unresolved transactions.",
                aiResults.Count,
                unresolvedTransactions.Length);
        }
        catch (TimeoutException)
        {
            logger.LogWarning(
                "AI categorization timed out for {UnresolvedCount} unresolved transactions.",
                unresolvedTransactions.Length);
            aiResults = unresolvedTransactions
                .Select(transaction => CreateUnresolvedResult(transaction, "AI categorization timed out."))
                .ToArray();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "AI categorization failed for {UnresolvedCount} unresolved transactions.",
                unresolvedTransactions.Length);
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
        var rememberedRules = new HashSet<string>(StringComparer.Ordinal);

        foreach (var result in acceptedAiResults)
        {
            if (result.NeedsReview
                || result.Source is CategorizationSource.Unresolved
                || result.CategoryId == BuiltInCategoryIds.Other
                || !transactionsById.TryGetValue(result.TransactionId, out var transaction))
            {
                continue;
            }

            var rulePrefix = NormalizeRulePrefix(result.LearnedRulePrefix);
            var merchantCode = NormalizeRulePrefix(merchantCodeResolver.Resolve(transaction));
            if (string.IsNullOrWhiteSpace(rulePrefix)
                || !merchantCode.StartsWith(rulePrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var ruleKey = $"{rulePrefix}|{result.CategoryId}";
            if (!rememberedRules.Add(ruleKey))
            {
                continue;
            }

            await categoryRuleRepository.AddAsync(
                new CategoryRule
                {
                    Pattern = rulePrefix,
                    CategoryId = result.CategoryId,
                    MatchType = CategoryRuleMatchType.Prefix
                },
                cancellationToken).ConfigureAwait(false);
        }
    }

    private IReadOnlyList<TransactionCategorization> CreateAiResultsForUnresolvedTransactions(
        IReadOnlyList<TransactionCategorization> representativeAiResults,
        IReadOnlyList<Transaction> representativeTransactions,
        IReadOnlyList<Transaction> unresolvedTransactions)
    {
        var representativeTransactionsById = representativeTransactions.ToDictionary(transaction => transaction.Id);
        var representativeResultsByMerchantCode = representativeAiResults
            .Where(result => representativeTransactionsById.ContainsKey(result.TransactionId))
            .ToDictionary(
                result => merchantCodeResolver.Resolve(representativeTransactionsById[result.TransactionId]),
                result => result,
                StringComparer.Ordinal);
        var learnedPrefixResults = representativeAiResults
            .Where(result => representativeTransactionsById.ContainsKey(result.TransactionId)
                && !result.NeedsReview
                && result.Source is not CategorizationSource.Unresolved
                && result.CategoryId != BuiltInCategoryIds.Other
                && !string.IsNullOrWhiteSpace(result.LearnedRulePrefix))
            .Select(result => new
            {
                Result = result,
                Prefix = NormalizeRulePrefix(result.LearnedRulePrefix),
                MerchantCode = NormalizeRulePrefix(merchantCodeResolver.Resolve(representativeTransactionsById[result.TransactionId]))
            })
            .Where(item => item.Prefix.Length > 0
                && item.MerchantCode.StartsWith(item.Prefix, StringComparison.Ordinal))
            .OrderByDescending(item => item.Prefix.Length)
            .ToArray();
        var results = new List<TransactionCategorization>();

        foreach (var transaction in unresolvedTransactions)
        {
            var merchantCode = NormalizeRulePrefix(merchantCodeResolver.Resolve(transaction));
            var learnedMatch = learnedPrefixResults
                .FirstOrDefault(item => merchantCode.StartsWith(item.Prefix, StringComparison.Ordinal));

            if (learnedMatch is not null)
            {
                results.Add(CopyResultForTransaction(learnedMatch.Result, transaction.Id, learnedMatch.Prefix));
                continue;
            }

            if (representativeResultsByMerchantCode.TryGetValue(merchantCode, out var representativeResult))
            {
                results.Add(CopyResultForTransaction(representativeResult, transaction.Id, representativeResult.LearnedRulePrefix));
            }
        }

        return results;
    }

    private static TransactionCategorization CopyResultForTransaction(
        TransactionCategorization result,
        Guid transactionId,
        string? learnedRulePrefix)
    {
        return result with
        {
            TransactionId = transactionId,
            LearnedRulePrefix = learnedRulePrefix
        };
    }

    private static string NormalizeRulePrefix(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();
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
