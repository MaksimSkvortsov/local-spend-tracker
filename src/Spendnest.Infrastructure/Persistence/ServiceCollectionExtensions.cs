using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spendnest.Core.Accounts;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Importing;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Accounts;
using Spendnest.Infrastructure.Categories;
using Spendnest.Infrastructure.Categorization;
using Spendnest.Infrastructure.Importing;
using Spendnest.Infrastructure.Transactions;

namespace Spendnest.Infrastructure.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSpendnestSqlitePersistence(
        this IServiceCollection services,
        string? connectionString = null)
    {
        services.AddDbContextFactory<SpendnestDbContext>(options =>
            options.UseSqlite(connectionString ?? SpendnestDataPaths.GetDefaultConnectionString()));

        services.AddSingleton<SpendnestDatabaseInitializer>();
        services.AddSingleton<ICardAccountRepository, SqliteCardAccountRepository>();
        services.AddSingleton<ICategoryRepository, SqliteCategoryRepository>();
        services.AddSingleton<ITransactionRepository, SqliteTransactionRepository>();
        services.AddSingleton<IStatementImportRepository, SqliteStatementImportRepository>();
        services.AddSingleton<ICategoryRuleRepository, SqliteCategoryRuleRepository>();
        services.AddSingleton<ITransactionCategoryAssignmentRepository, SqliteTransactionCategoryAssignmentRepository>();

        return services;
    }
}
