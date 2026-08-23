using Spendnest.Desktop.Services;

namespace Spendnest.UiHarness;

public sealed class DevStatementFilePicker : IStatementFilePicker
{
    public Task<PickedStatementFile?> PickCsvAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<PickedStatementFile?>(null);
    }
}
