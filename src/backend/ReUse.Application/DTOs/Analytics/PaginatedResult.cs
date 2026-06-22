namespace ReUse.Application.DTOs.Analytics;

public record PaginatedResult<T>
{
    public List<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }

    public static PaginatedResult<T> Create(IReadOnlyList<T> allItems, int page, int pageSize)
    {
        var totalCount = allItems.Count;
        var totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;
        var items = allItems.Skip(page * pageSize).Take(pageSize).ToList();

        return new PaginatedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
        };
    }
}