namespace Spendnest.Application.Reporting;

public sealed record TopMerchantLine(
    string DisplayName,
    int TransactionCount,
    decimal Amount);
