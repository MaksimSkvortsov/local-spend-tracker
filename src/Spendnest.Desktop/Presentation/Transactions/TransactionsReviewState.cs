using Spendnest.Core.Categorization;

namespace Spendnest.Desktop.Presentation.Transactions;

public sealed record TransactionsReviewState(
    IReadOnlyDictionary<Guid, TransactionCategoryAssignment> AssignmentsByTransactionId,
    int ReviewCount);
