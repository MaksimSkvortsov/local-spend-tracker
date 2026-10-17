using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;

namespace Spendnest.Core.Importing;

/// <summary>
/// Persists a finished statement import with its transactions and category assignments together.
/// </summary>
public interface IStatementImportStore
{
    Task CompleteImportAsync(
        StatementImport statementImport,
        IReadOnlyList<Transaction> transactions,
        IReadOnlyList<TransactionCategoryAssignment> assignments,
        CancellationToken cancellationToken);
}
