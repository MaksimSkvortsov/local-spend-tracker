namespace Spendnest.Core.Categorization;

/// <summary>
/// Defines how a local category rule matches transaction description text.
/// </summary>
public enum CategoryRuleMatchType
{
    Exact = 1,
    Prefix = 2,
    Contains = 3
}
