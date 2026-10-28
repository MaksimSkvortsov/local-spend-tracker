namespace Spendnest.UiHarness.Tests.Seeding;

using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spendnest.Core.Ai;
using Spendnest.Core.Credentials;
using Spendnest.Desktop.Services;
using Spendnest.Desktop.State;
using Spendnest.Infrastructure.Categorization;
using Spendnest.Infrastructure.Credentials;
using Spendnest.Infrastructure.Persistence;
using Spendnest.UiHarness.Ai;
using Spendnest.UiHarness.Seeding;

public sealed class HarnessSeederTests : IDisposable
{
    private readonly string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly ServiceProvider serviceProvider;

    public HarnessSeederTests()
    {
        Directory.CreateDirectory(tempDirectory);
        serviceProvider = new ServiceCollection()
            .AddSpendnestSqlitePersistence($"Data Source={Path.Combine(tempDirectory, "spendnest.db")}")
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddSingleton<ICredentialStore, InMemoryCredentialStore>()
            .AddSingleton<IAiConnectionTestService, FakeAiConnectionTestService>()
            .AddSingleton<OpenAiCategorizerOptions>()
            .AddSingleton<SettingsPageService>()
            .AddSingleton<DashboardPeriodState>()
            .AddSingleton<DevStatementFilePicker>()
            .AddSingleton<AppDataRefreshNotifier>()
            .AddSingleton<HarnessSeeder>()
            .BuildServiceProvider();
    }

    [Fact]
    public async Task SeedAsync_WithSettingsAndThreeCards_ShouldSaveDataAndApiKey()
    {
        await InitializeDatabaseAsync();

        var result = await SeedAsync(new HarnessSeedRequest(true, ["sapphire", "capone", "boa"]));

        result.Cards.Should().HaveCount(3);
        await using var dbContext = await CreateDbContextAsync();
        (await dbContext.CardAccounts.CountAsync()).Should().Be(3);
        (await dbContext.StatementImports.CountAsync()).Should().Be(6);
        (await dbContext.Transactions.CountAsync()).Should().Be(62);
        (await dbContext.CategoryRules.CountAsync()).Should().Be(1);
        (await GetApiKeyAsync()).Should().Be(HarnessSeeder.TestApiKey);
    }

    [Fact]
    public async Task SeedAsync_AsNewUserAfterSeeding_ShouldResetDataSettingsAndUiState()
    {
        await InitializeDatabaseAsync();
        await SeedAsync(new HarnessSeedRequest(true, ["sapphire"]));
        var periodState = serviceProvider.GetRequiredService<DashboardPeriodState>();
        periodState.Mode = "Month";
        periodState.Year = 2026;
        periodState.Month = 1;
        var statementFile = Path.Combine(tempDirectory, "statement.csv");
        await File.WriteAllTextAsync(statementFile, "Date,Description,Amount");
        var filePicker = serviceProvider.GetRequiredService<DevStatementFilePicker>();
        filePicker.Select(statementFile);

        var result = await SeedAsync(new HarnessSeedRequest(false, []));

        result.Should().BeEquivalentTo(new HarnessSeedResult(false, []));
        await using var dbContext = await CreateDbContextAsync();
        (await dbContext.CardAccounts.AnyAsync()).Should().BeFalse();
        (await dbContext.Transactions.AnyAsync()).Should().BeFalse();
        (await dbContext.CategoryRules.AnyAsync()).Should().BeFalse();
        (await dbContext.Categories.AnyAsync()).Should().BeTrue();
        (await GetApiKeyAsync()).Should().BeNull();
        periodState.Should().BeEquivalentTo(new DashboardPeriodState());
        (await filePicker.PickCsvAsync(CancellationToken.None)).Should().BeNull();
    }

    public void Dispose()
    {
        serviceProvider.Dispose();
        SqliteConnection.ClearAllPools();
        Directory.Delete(tempDirectory, recursive: true);
    }

    private Task InitializeDatabaseAsync()
    {
        return serviceProvider
            .GetRequiredService<SpendnestDatabaseInitializer>()
            .InitializeAsync(CancellationToken.None);
    }

    private Task<HarnessSeedResult> SeedAsync(HarnessSeedRequest request)
    {
        return serviceProvider
            .GetRequiredService<HarnessSeeder>()
            .SeedAsync(HarnessSeedPlan.FromRequest(request), CancellationToken.None);
    }

    private Task<SpendnestDbContext> CreateDbContextAsync()
    {
        return serviceProvider
            .GetRequiredService<IDbContextFactory<SpendnestDbContext>>()
            .CreateDbContextAsync();
    }

    private Task<string?> GetApiKeyAsync()
    {
        return serviceProvider
            .GetRequiredService<ICredentialStore>()
            .GetStringAsync(CredentialKeys.OpenAiApiKey, CancellationToken.None);
    }
}
