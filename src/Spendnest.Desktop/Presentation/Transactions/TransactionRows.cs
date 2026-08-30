using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;

namespace Spendnest.Desktop.Presentation.Transactions;

public static class TransactionRows
{
    public static IReadOnlyList<TransactionRow> FromTransactions(
        IReadOnlyList<Transaction> transactions,
        IReadOnlyDictionary<Guid, TransactionCategoryAssignment> assignmentsByTransactionId,
        IReadOnlyDictionary<int, string> categoryColorsById,
        IReadOnlyDictionary<Guid, string> cardNamesById)
    {
        return transactions
            .Select(transaction => FromTransaction(
                transaction,
                assignmentsByTransactionId,
                categoryColorsById,
                cardNamesById))
            .ToArray();
    }

    private static TransactionRow FromTransaction(
        Transaction transaction,
        IReadOnlyDictionary<Guid, TransactionCategoryAssignment> assignmentsByTransactionId,
        IReadOnlyDictionary<int, string> categoryColorsById,
        IReadOnlyDictionary<Guid, string> cardNamesById)
    {
        var assignment = assignmentsByTransactionId.GetValueOrDefault(transaction.Id);
        var categoryId = assignment?.CategoryId ?? BuiltInCategoryIds.Other;

        return new TransactionRow(
            transaction.Id,
            transaction.CardAccountId,
            transaction.PostedDate,
            transaction.OriginalDescription,
            categoryId,
            categoryColorsById.GetValueOrDefault(categoryId, "#e5e7e2"),
            cardNamesById.GetValueOrDefault(transaction.CardAccountId, "Unknown Card"),
            transaction.Amount,
            assignment?.NeedsReview ?? false);
    }
}
