using Spendnest.Desktop.Presentation.Shared;

namespace Spendnest.Desktop.Presentation.Importing;

public sealed class ImportHistoryState
{
    private const int PageSize = 10;

    public IReadOnlyList<UploadHistoryItem> Rows { get; private set; } = [];

    public int CurrentPage { get; private set; } = 1;

    public PagedList<UploadHistoryItem> Page =>
        PagedList<UploadHistoryItem>.Create(Rows, CurrentPage, PageSize);

    public IReadOnlyList<UploadHistoryItem> PagedRows => Page.Items;

    public int PageCount => Page.PageCount;

    public void ReplaceRows(IReadOnlyList<UploadHistoryItem> rows)
    {
        Rows = rows;
        CurrentPage = 1;
    }

    public void PreviousPage()
    {
        CurrentPage = Math.Max(1, CurrentPage - 1);
    }

    public void NextPage()
    {
        CurrentPage = Math.Min(PageCount, CurrentPage + 1);
    }
}
