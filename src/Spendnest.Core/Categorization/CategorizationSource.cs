namespace Spendnest.Core.Categorization;

/// <summary>
/// Identifies where a transaction category decision came from.
/// </summary>
public enum CategorizationSource
{
    LocalRules = 1,
    FakeAi = 2,
    OpenAi = 3,
    Unresolved = 4
}
