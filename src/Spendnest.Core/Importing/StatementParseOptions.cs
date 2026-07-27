namespace Spendnest.Core.Importing;

/// <summary>
/// Provides CSV statement parser settings and optional column mappings.
/// </summary>
public sealed record StatementParseOptions(
    string? DateColumnName = null,
    string? DescriptionColumnName = null,
    string? CategoryColumnName = null,
    string? AmountColumnName = null,
    string? DebitColumnName = null,
    string? CreditColumnName = null,
    string? DateFormat = null,
    bool SignedAmountExpensesAreNegative = true,
    bool PreviewOnly = false,
    int? PreviewRowLimit = null);
