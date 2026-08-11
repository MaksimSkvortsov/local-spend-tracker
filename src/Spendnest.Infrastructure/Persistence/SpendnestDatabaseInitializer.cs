using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Spendnest.Core.Categories;

namespace Spendnest.Infrastructure.Persistence;

public sealed class SpendnestDatabaseInitializer
{
    private readonly IDbContextFactory<SpendnestDbContext> dbContextFactory;

    public SpendnestDatabaseInitializer(IDbContextFactory<SpendnestDbContext> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        await MarkExistingInitialSchemaAsync(dbContext, cancellationToken).ConfigureAwait(false);
        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        await SeedBuiltInCategoriesAsync(dbContext, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteUserDataAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        await dbContext.TransactionCategoryAssignments.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.CategoryRules.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.Transactions.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.StatementImports.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.CardAccounts.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await SeedBuiltInCategoriesAsync(dbContext, cancellationToken).ConfigureAwait(false);
    }

    private static async Task MarkExistingInitialSchemaAsync(
        SpendnestDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var hasInitialTable = await TableExistsAsync(dbContext, "CardAccounts", cancellationToken)
            .ConfigureAwait(false);
        var hasMigrationHistory = await TableExistsAsync(dbContext, "__EFMigrationsHistory", cancellationToken)
            .ConfigureAwait(false);

        if (!hasInitialTable || hasMigrationHistory)
        {
            return;
        }

        var initialMigrationId = dbContext
            .GetService<IMigrationsAssembly>()
            .Migrations
            .Keys
            .First(migration => migration.EndsWith("_Initial", StringComparison.Ordinal));

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            );
            """,
            cancellationToken)
            .ConfigureAwait(false);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT OR IGNORE INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ({initialMigrationId}, {"10.0.10"});
            """,
            cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<bool> TableExistsAsync(
        SpendnestDbContext dbContext,
        string tableName,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            SELECT 1
            FROM sqlite_master
            WHERE type = 'table'
                AND name = $tableName
            LIMIT 1;
            """;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "$tableName";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is not null;
    }

    private static async Task SeedBuiltInCategoriesAsync(
        SpendnestDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var existingCategories = await dbContext.Categories
            .ToDictionaryAsync(category => category.Id, cancellationToken)
            .ConfigureAwait(false);

        foreach (var builtInCategory in BuiltInCategories.All)
        {
            if (existingCategories.TryGetValue(builtInCategory.Id, out var category))
            {
                category.Name = builtInCategory.Name;
                category.SortOrder = builtInCategory.SortOrder;
                category.IsActive = true;
                continue;
            }

            dbContext.Categories.Add(new Category
            {
                Id = builtInCategory.Id,
                Name = builtInCategory.Name,
                SortOrder = builtInCategory.SortOrder,
                IsActive = true
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
