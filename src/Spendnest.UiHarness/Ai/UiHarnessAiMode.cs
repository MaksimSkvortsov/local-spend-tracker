namespace Spendnest.UiHarness.Ai;

/// <summary>
/// Chooses the AI services the harness registers. <see cref="OpenAi"/> is the default for manual checks;
/// browser tests set <c>UiHarness:Ai</c> to <see cref="Fake"/> so they never call OpenAI.
/// </summary>
public enum UiHarnessAiMode
{
    OpenAi,
    Fake
}

public static class UiHarnessAiModeConfiguration
{
    public const string AiModeKey = "UiHarness:Ai";

    public static UiHarnessAiMode FromConfiguration(IConfiguration configuration)
    {
        var configuredMode = configuration[AiModeKey];

        if (string.IsNullOrWhiteSpace(configuredMode))
        {
            return UiHarnessAiMode.OpenAi;
        }

        return Enum.TryParse<UiHarnessAiMode>(configuredMode, ignoreCase: true, out var mode) && Enum.IsDefined(mode)
            ? mode
            : throw new InvalidOperationException(
                $"{AiModeKey} must be '{UiHarnessAiMode.OpenAi}' or '{UiHarnessAiMode.Fake}': '{configuredMode}'.");
    }
}
