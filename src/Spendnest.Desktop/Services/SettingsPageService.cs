using System.Diagnostics;
using Spendnest.Core.Ai;
using Spendnest.Core.Credentials;
using Spendnest.Desktop.Presentation.Settings;
using Spendnest.Infrastructure.Categorization;
using Spendnest.Infrastructure.Persistence;

namespace Spendnest.Desktop.Services;

public sealed class SettingsPageService(
    IAiConnectionTestService aiConnectionTestService,
    ICredentialStore credentialStore,
    SpendnestDatabaseInitializer databaseInitializer,
    OpenAiCategorizerOptions openAiOptions)
{
    private const string DefaultModel = "gpt-5.6-luna";

    private const int DefaultRequestTimeoutSeconds = 25;

    public string StorageLocation => SpendnestDataPaths.GetDefaultDatabasePath();

    public string LogsFolderLocation =>
        Path.GetDirectoryName(SpendnestDataPaths.GetDefaultLogPath())
        ?? SpendnestDataPaths.GetDefaultLogPath();

    public async Task<SettingsPageData> LoadAsync(CancellationToken cancellationToken)
    {
        var storedApiKey = await credentialStore.GetStringAsync(CredentialKeys.OpenAiApiKey, cancellationToken);

        return new SettingsPageData(
            !string.IsNullOrWhiteSpace(storedApiKey),
            openAiOptions.Model,
            Math.Max(1, (int)Math.Round(openAiOptions.RequestTimeout.TotalSeconds)));
    }

    public async Task<SettingsOperationResult> TestConnectionAsync(
        SettingsAiConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        var hasKey = !string.IsNullOrWhiteSpace(request.ApiKey) || request.HasConfiguredApiKey;
        if (!hasKey)
        {
            return Error("Enter an API key before testing.");
        }

        if (!IsValidModel(request.Model))
        {
            return Error("Choose a ChatGPT model.");
        }

        var result = await aiConnectionTestService.TestOpenAiAsync(
            new AiConnectionTestRequest(
                request.ApiKey,
                request.Model,
                TimeSpan.FromSeconds(request.RequestTimeoutSeconds)),
            cancellationToken);

        return new SettingsOperationResult(
            result.Message,
            result.Succeeded ? SettingsStatusKind.Success : SettingsStatusKind.Error);
    }

    public async Task<SettingsOperationResult> SaveAiConfigurationAsync(
        SettingsAiConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsValidModel(request.Model))
        {
            return Error("Choose a ChatGPT model.");
        }

        if (request.RequestTimeoutSeconds is < 5 or > 180)
        {
            return Error("Timeout must be between 5 and 180 seconds.");
        }

        if (!string.IsNullOrWhiteSpace(request.ApiKey))
        {
            await credentialStore.SaveStringAsync(CredentialKeys.OpenAiApiKey, request.ApiKey, cancellationToken);
        }
        else if (!request.HasConfiguredApiKey)
        {
            return Error("API key is required.");
        }

        openAiOptions.Model = request.Model;
        openAiOptions.RequestTimeout = TimeSpan.FromSeconds(request.RequestTimeoutSeconds);

        return Success("AI configuration saved.");
    }

    public async Task<SettingsOperationResult> DeleteConfigurationAsync(CancellationToken cancellationToken)
    {
        await credentialStore.ClearAsync(CredentialKeys.OpenAiApiKey, cancellationToken);
        openAiOptions.Model = DefaultModel;
        openAiOptions.RequestTimeout = TimeSpan.FromSeconds(DefaultRequestTimeoutSeconds);

        return Success("AI connection details deleted.");
    }

    public async Task<SettingsOperationResult> DeleteLocalDataAsync(CancellationToken cancellationToken)
    {
        await databaseInitializer.DeleteUserDataAsync(cancellationToken);

        return Success("Local data deleted.");
    }

    public void OpenLogsFolder()
    {
        Directory.CreateDirectory(LogsFolderLocation);
        Process.Start(new ProcessStartInfo
        {
            FileName = LogsFolderLocation,
            UseShellExecute = true
        });
    }

    private static SettingsOperationResult Success(string message)
    {
        return new SettingsOperationResult(message, SettingsStatusKind.Success);
    }

    private static SettingsOperationResult Error(string message)
    {
        return new SettingsOperationResult(message, SettingsStatusKind.Error);
    }

    private static bool IsValidModel(string value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.StartsWith("gpt-", StringComparison.OrdinalIgnoreCase);
    }
}
