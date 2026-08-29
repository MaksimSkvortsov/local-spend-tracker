namespace Spendnest.Desktop.Presentation.Dashboard;

public sealed record BiggestTransactionRow(
    string Merchant,
    string Category,
    string CategoryColor,
    decimal Amount);
