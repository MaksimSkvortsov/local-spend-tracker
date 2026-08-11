namespace Spendnest.Infrastructure.Tests.Persistence;

using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Spendnest.Core.Accounts;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Importing;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Persistence;

public class SqlitePersistenceTests
{
    [Fact]
    public async Task SqliteRepositories_ShouldPersistDataAcrossRepositoryInstances()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var databasePath = Path.Combine(tempDirectory, "spendnest-test.db");

        try
        {
            using var serviceProvider = new ServiceCollection()
                .AddSpendnestSqlitePersistence($"Data Source={databasePath}")
                .BuildServiceProvider();

            await serviceProvider
                .GetRequiredService<SpendnestDatabaseInitializer>()
                .InitializeAsync(CancellationToken.None);

            var cardAccounts = serviceProvider.GetRequiredService<ICardAccountRepository>();
            var statementImports = serviceProvider.GetRequiredService<IStatementImportRepository>();
            var transactions = serviceProvider.GetRequiredService<ITransactionRepository>();
            var assignments = serviceProvider.GetRequiredService<ITransactionCategoryAssignmentRepository>();
            var cardAccount = await cardAccounts.CreateAsync("Family Visa", CancellationToken.None);
            var statementImport = new StatementImport
            {
                CardAccountId = cardAccount.Id,
                FilePath = "statement.csv",
                FileName = "statement.csv",
                FileHash = "ABC123",
                Status = StatementImportStatus.Completed,
                StartedAtUtc = new DateTimeOffset(2026, 7, 18, 12, 0, 0, TimeSpan.Zero),
                CompletedAtUtc = new DateTimeOffset(2026, 7, 18, 12, 1, 0, TimeSpan.Zero)
            };
            await statementImports.AddAsync(statementImport, CancellationToken.None);
            var newerStatementImport = new StatementImport
            {
                CardAccountId = cardAccount.Id,
                FilePath = "newer-statement.csv",
                FileName = "newer-statement.csv",
                FileHash = "DEF456",
                Status = StatementImportStatus.Completed,
                StartedAtUtc = new DateTimeOffset(2026, 7, 19, 12, 0, 0, TimeSpan.Zero),
                CompletedAtUtc = new DateTimeOffset(2026, 7, 19, 12, 1, 0, TimeSpan.Zero)
            };
            await statementImports.AddAsync(newerStatementImport, CancellationToken.None);

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                CardAccountId = cardAccount.Id,
                StatementImportId = statementImport.Id,
                PostedDate = new DateOnly(2026, 7, 18),
                OriginalDescription = "BULK MART #0218 RIVERTON VA",
                Amount = 141.83m,
                SourceRowNumber = 2,
                ImportedAtUtc = DateTimeOffset.UtcNow
            };
            await transactions.AddRangeAsync([transaction], CancellationToken.None);
            await assignments.SaveAsync(
                new TransactionCategoryAssignment
                {
                    TransactionId = transaction.Id,
                    CategoryId = BuiltInCategoryIds.Groceries,
                    Confidence = 1m,
                    NeedsReview = false,
                    Source = CategorizationSource.LocalRules,
                    Explanation = "Matched learned merchant rule."
                },
                CancellationToken.None);

            using var secondServiceProvider = new ServiceCollection()
                .AddSpendnestSqlitePersistence($"Data Source={databasePath}")
                .BuildServiceProvider();

            await secondServiceProvider
                .GetRequiredService<SpendnestDatabaseInitializer>()
                .InitializeAsync(CancellationToken.None);

            var persistedTransactions = await secondServiceProvider
                .GetRequiredService<ITransactionRepository>()
                .ListAsync(CancellationToken.None);
            var persistedAssignment = await secondServiceProvider
                .GetRequiredService<ITransactionCategoryAssignmentRepository>()
                .GetByTransactionIdAsync(transaction.Id, CancellationToken.None);
            var persistedCategories = await secondServiceProvider
                .GetRequiredService<ICategoryRepository>()
                .ListAsync(CancellationToken.None);
            var persistedImports = await secondServiceProvider
                .GetRequiredService<IStatementImportRepository>()
                .ListAsync(CancellationToken.None);

            persistedTransactions.Should().ContainSingle()
                .Which.OriginalDescription.Should().Be("BULK MART #0218 RIVERTON VA");
            persistedAssignment.Should().NotBeNull();
            persistedAssignment!.CategoryId.Should().Be(BuiltInCategoryIds.Groceries);
            persistedCategories.Should().Contain(category =>
                category.Id == BuiltInCategoryIds.Groceries
                && category.ColorHex == "#69c145");
            persistedImports.Select(item => item.FileName)
                .Should().Equal("newer-statement.csv", "statement.csv");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
