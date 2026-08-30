using Spendnest.Core.Accounts;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;

namespace Spendnest.Desktop.Presentation.Transactions;

public sealed record TransactionsPageData(
    IReadOnlyList<Transaction> Transactions,
    IReadOnlyList<CardAccount> Cards,
    IReadOnlyList<BuiltInCategory> Categories,
    IReadOnlyDictionary<Guid, TransactionCategoryAssignment> AssignmentsByTransactionId,
    IReadOnlyDictionary<int, string> CategoryColorsById,
    IReadOnlyDictionary<Guid, string> CardNamesById,
    int ReviewCount);
