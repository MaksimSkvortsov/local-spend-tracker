namespace Spendnest.Core.Categorization;

/// <summary>
/// Identifies where a transaction category result came from.
/// </summary>
public enum CategorizationSource
{
    LocalRules = 1,
    LocalAi = 2,
    Ai = 3,
    Unresolved = 4
}
