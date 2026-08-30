using Spendnest.Core.Transactions;

namespace Spendnest.Desktop.Presentation.Transactions;

public sealed record TransactionCategoryChange(
    Transaction Transaction,
    string? CategoryId);
