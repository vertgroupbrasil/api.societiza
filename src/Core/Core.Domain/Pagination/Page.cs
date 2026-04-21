namespace Core.Domain.Pagination;

public record Page
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}