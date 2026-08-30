using Spendnest.Core.Accounts;
using Spendnest.Core.Categories;

namespace Spendnest.Desktop.Presentation.Transactions;

public sealed record TransactionsPageData(
    IReadOnlyList<TransactionRow> Transactions,
    IReadOnlyList<CardAccount> Cards,
    IReadOnlyList<BuiltInCategory> Categories,
    int ReviewCount);
