namespace Spendnest.Infrastructure.Categorization;

/// <summary>
/// Holds OpenAI API settings for transaction categorization.
/// </summary>
public sealed class OpenAiCategorizerOptions
{
    public string? ApiKey { get; set; }

    public string Model { get; set; } = "gpt-5.6-luna";

    public Uri Endpoint { get; set; } = new("https://api.openai.com/v1/responses");

    public decimal ReviewConfidenceThreshold { get; set; } = 0.75m;

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(25);

    public int MaxTransactionsPerRequest { get; set; } = 50;
}
