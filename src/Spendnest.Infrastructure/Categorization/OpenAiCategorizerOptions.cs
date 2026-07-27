namespace Spendnest.Infrastructure.Categorization;

/// <summary>
/// Holds OpenAI API settings for transaction categorization.
/// </summary>
public sealed class OpenAiCategorizerOptions
{
    public string? ApiKey { get; init; }

    public string Model { get; init; } = "gpt-5.6-luna";

    public Uri Endpoint { get; init; } = new("https://api.openai.com/v1/responses");

    public decimal ReviewConfidenceThreshold { get; init; } = 0.75m;

    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(25);

    public int MaxTransactionsPerRequest { get; init; } = 50;
}
