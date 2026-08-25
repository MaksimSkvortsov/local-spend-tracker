namespace Spendnest.Desktop.Presentation.Shared;

public sealed record PagedList<T>(
    IReadOnlyList<T> Items,
    int CurrentPage,
    int PageSize,
    int TotalCount)
{
    public int PageCount => Math.Max(1, (int)Math.Ceiling(TotalCount / (decimal)PageSize));

    public static PagedList<T> Create(IReadOnlyList<T> source, int currentPage, int pageSize)
    {
        if (pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Page size must be greater than zero.");
        }

        var totalCount = source.Count;
        var pageCount = Math.Max(1, (int)Math.Ceiling(totalCount / (decimal)pageSize));
        var normalizedPage = Math.Clamp(currentPage, 1, pageCount);
        var items = source
            .Skip((normalizedPage - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        return new PagedList<T>(items, normalizedPage, pageSize, totalCount);
    }
}
