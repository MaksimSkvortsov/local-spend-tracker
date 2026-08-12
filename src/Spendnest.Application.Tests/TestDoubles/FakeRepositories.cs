namespace Spendnest.Application.Tests.TestDoubles;

using Spendnest.Core.Accounts;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Importing;
using Spendnest.Core.Transactions;

public sealed class FakeCardAccountRepository : ICardAccountRepository
{
    private readonly List<CardAccount> cardAccounts = [];

    public Task<CardAccount?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken)
    {
        var normalizedName = NormalizeName(name);
        var account = cardAccounts.FirstOrDefault(item =>
            string.Equals(item.Name, normalizedName, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(account);
    }

    public Task<CardAccount> CreateAsync(
        string name,
        CancellationToken cancellationToken)
    {
        var account = new CardAccount
        {
            Name = NormalizeName(name),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        cardAccounts.Add(account);

        return Task.FromResult(account);
    }

    public Task<IReadOnlyList<CardAccount>> ListAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<CardAccount>>(cardAccounts.ToArray());
    }

    private static string NormalizeName(string name)
    {
        return string.IsNullOrWhiteSpace(name)
            ? "Default Card"
            : name.Trim();
    }
}

public sealed class FakeCategoryRepository : ICategoryRepository
{
    public Task<IReadOnlyList<BuiltInCategory>> ListAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<BuiltInCategory>>(
            BuiltInCategories.All
                .OrderBy(category => category.SortOrder)
                .ToArray());
    }

    public Task<BuiltInCategory?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(BuiltInCategories.All.FirstOrDefault(category => category.Id == id));
    }
}

public sealed class FakeCategoryRuleRepository : ICategoryRuleRepository
{
    private readonly List<CategoryRule> rules = [];

    public Task AddAsync(
        CategoryRule rule,
        CancellationToken cancellationToken)
    {
        rules.Add(rule);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CategoryRule>> ListAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<CategoryRule>>(rules.ToArray());
    }
}

public sealed class FakeStatementImportRepository : IStatementImportRepository
{
    private readonly List<StatementImport> statementImports = [];

    public Task AddAsync(
        StatementImport statementImport,
        CancellationToken cancellationToken)
    {
        statementImports.Add(statementImport);

        return Task.CompletedTask;
    }

    public Task UpdateAsync(
        StatementImport statementImport,
        CancellationToken cancellationToken)
    {
        var index = statementImports.FindIndex(item => item.Id == statementImport.Id);
        if (index < 0)
        {
            statementImports.Add(statementImport);
            return Task.CompletedTask;
        }

        statementImports[index] = statementImport;

        return Task.CompletedTask;
    }

    public Task<StatementImport?> GetByFileHashAsync(
        string fileHash,
        CancellationToken cancellationToken)
    {
        var statementImport = statementImports.FirstOrDefault(item =>
            item.FileHash.Equals(fileHash, StringComparison.OrdinalIgnoreCase)
            && item.Status != StatementImportStatus.Failed);

        return Task.FromResult(statementImport);
    }

    public Task<IReadOnlyList<StatementImport>> ListAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<StatementImport>>(
            statementImports
                .OrderByDescending(item => item.StartedAtUtc)
                .ToArray());
    }
}

public sealed class FakeTransactionCategoryAssignmentRepository : ITransactionCategoryAssignmentRepository
{
    private readonly Dictionary<Guid, TransactionCategoryAssignment> assignments = [];

    public Task SaveAsync(
        TransactionCategoryAssignment assignment,
        CancellationToken cancellationToken)
    {
        assignments[assignment.TransactionId] = assignment;

        return Task.CompletedTask;
    }

    public Task<TransactionCategoryAssignment?> GetByTransactionIdAsync(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(assignments.GetValueOrDefault(transactionId));
    }

    public Task<IReadOnlyList<TransactionCategoryAssignment>> ListAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<TransactionCategoryAssignment>>(assignments.Values.ToArray());
    }
}

public sealed class FakeTransactionRepository : ITransactionRepository
{
    private readonly List<Transaction> transactions = [];

    public Task AddRangeAsync(
        IReadOnlyList<Transaction> transactionsToAdd,
        CancellationToken cancellationToken)
    {
        transactions.AddRange(transactionsToAdd);

        return Task.CompletedTask;
    }

    public Task<Transaction?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(transactions.FirstOrDefault(transaction => transaction.Id == id));
    }

    public Task<IReadOnlyList<Transaction>> ListAsync(CancellationToken cancellationToken)
    {
        return ListAsync(new TransactionQuery(), cancellationToken);
    }

    public Task<IReadOnlyList<Transaction>> ListAsync(
        TransactionQuery query,
        CancellationToken cancellationToken)
    {
        var result = transactions
            .Where(transaction => query.StartDate is null || transaction.PostedDate >= query.StartDate)
            .Where(transaction => query.EndDate is null || transaction.PostedDate <= query.EndDate)
            .OrderBy(transaction => transaction.PostedDate)
            .ThenBy(transaction => transaction.OriginalDescription)
            .ToArray();

        return Task.FromResult<IReadOnlyList<Transaction>>(result);
    }
}
