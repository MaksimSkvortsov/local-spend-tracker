using Microsoft.Extensions.DependencyInjection;
using Spendnest.Application.Categorization;
using Spendnest.Application.Importing;
using Spendnest.Application.Reporting;
using Spendnest.Application.Review;
using Spendnest.Core.Categorization;
using Spendnest.Core.Importing;
using Spendnest.Core.Reporting;
using Spendnest.Core.Review;

namespace Spendnest.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSpendnestApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IStatementFileImportService, StatementFileImportService>();
        services.AddSingleton<ITransactionCategorizationService, TransactionCategorizationService>();
        services.AddSingleton<ITransactionCategorizationApplier, TransactionCategorizationApplier>();
        services.AddSingleton<CategorySpendingReportBuilder>();
        services.AddSingleton<ICategorySpendingReportService, CategorySpendingReportService>();
        services.AddSingleton<ITransactionReviewService, TransactionReviewService>();

        return services;
    }
}
