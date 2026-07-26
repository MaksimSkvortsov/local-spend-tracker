using Spendnest.Core.Transactions;

namespace Spendnest.Core.Categorization;

/// <summary>
/// Maps a transaction to one of Spendnest's built-in category ids.
/// </summary>
public interface ITransactionCategoryMapper
{
    int MapCategoryId(Transaction transaction);
}
