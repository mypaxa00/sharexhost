namespace ShareXHost;

public sealed class PaginatedResponse<T>
{
    public PaginatedResponse(List<T> items, int totalCount, int page, int pageSize)
    {
        Items = [.. items];
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<T> Items { get; set; }
}