namespace Spendnest.App.State;

/// <summary>
/// Notifies active pages when imported transaction data changes.
/// </summary>
public sealed class AppDataRefreshNotifier
{
    public event Action<DateOnly?>? TransactionsChanged;

    public void NotifyTransactionsChanged(DateOnly? focusDate = null)
    {
        TransactionsChanged?.Invoke(focusDate);
    }
}
