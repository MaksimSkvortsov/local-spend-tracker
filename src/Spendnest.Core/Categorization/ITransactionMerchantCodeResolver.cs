using Spendnest.Core.Transactions;

namespace Spendnest.Core.Categorization;

/// <summary>
/// Builds a stable merchant code used for learned categorization rules.
/// </summary>
public interface ITransactionMerchantCodeResolver
{
    string Resolve(Transaction transaction);
}
