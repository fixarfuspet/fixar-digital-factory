namespace Fixar.Application.Common.Models;

/// <summary>
/// Generic page of results for future list endpoints. Kept here as a
/// ready-made building block; no business module uses it yet.
/// </summary>
public class PaginatedList<T>
{
    public IReadOnlyList<T> Items { get; }

    public int PageNumber { get; }

    public int TotalPages { get; }

    public int TotalCount { get; }

    public PaginatedList(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        TotalCount = totalCount;
        TotalPages = pageSize == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        Items = items;
    }

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;
}
