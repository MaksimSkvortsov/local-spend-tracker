using Spendnest.Core.Transactions;

namespace Spendnest.Core.Categorization;

/// <summary>
/// Maps a transaction to one of Spendnest's built-in category codes.
/// </summary>
public interface ITransactionCategoryMapper
{
    string MapCategoryCode(Transaction transaction);
}
