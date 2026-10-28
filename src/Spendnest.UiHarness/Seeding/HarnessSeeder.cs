using Microsoft.EntityFrameworkCore;
using Spendnest.Desktop.Presentation.Settings;
using Spendnest.Desktop.Services;
using Spendnest.Desktop.State;
using Spendnest.Infrastructure.Categorization;
using Spendnest.Infrastructure.Persistence;

namespace Spendnest.UiHarness.Seeding;

/// <summary>
/// Resets the harness to a fresh install, then applies a seed plan. Local data and AI settings are reset
/// through <see cref="SettingsPageService"/>, the same path the Settings page uses.
/// </summary>
public sealed class HarnessSeeder(
    IDbContextFactory<SpendnestDbContext> dbContextFactory,
    SettingsPageService settingsPageService,
    OpenAiCategorizerOptions openAiOptions,
    DashboardPeriodState dashboardPeriodState,
    DevStatementFilePicker statementFilePicker,
    AppDataRefreshNotifier refreshNotifier)
{
    public const string TestApiKey = "e2e-test-key";

    public async Task<HarnessSeedResult> SeedAsync(HarnessSeedPlan plan, CancellationToken cancellationToken)
    {
        await ResetAsync(cancellationToken);

        if (plan.Settings)
        {
            await ConfigureAiAsync(cancellationToken);
        }

        var data = HarnessSeedData.Build(plan.Cards);
        await SaveAsync(data, cancellationToken);
        refreshNotifier.NotifyTransactionsChanged();

        return data.ToResult(plan.Settings);
    }

    private async Task ResetAsync(CancellationToken cancellationToken)
    {
        await settingsPageService.DeleteLocalDataAsync(cancellationToken);
        await settingsPageService.DeleteConfigurationAsync(cancellationToken);

        var defaultPeriod = new DashboardPeriodState();
        dashboardPeriodState.Mode = defaultPeriod.Mode;
        dashboardPeriodState.Year = defaultPeriod.Year;
        dashboardPeriodState.Month = defaultPeriod.Month;

        statementFilePicker.Clear();
    }

    private async Task ConfigureAiAsync(CancellationToken cancellationToken)
    {
        var result = await settingsPageService.SaveAiConfigurationAsync(
            new SettingsAiConfigurationRequest(
                TestApiKey,
                HasConfiguredApiKey: false,
                openAiOptions.Model,
                (int)openAiOptions.RequestTimeout.TotalSeconds),
            cancellationToken);

        if (result.StatusKind == SettingsStatusKind.Error)
        {
            throw new InvalidOperationException($"Seeding AI settings failed: {result.Message}");
        }
    }

    private async Task SaveAsync(HarnessSeedData data, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        dbContext.CardAccounts.AddRange(data.Cards);
        dbContext.CategoryRules.AddRange(data.Rules);
        dbContext.StatementImports.AddRange(data.Imports);
        dbContext.Transactions.AddRange(data.Transactions);
        dbContext.TransactionCategoryAssignments.AddRange(data.Assignments);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
