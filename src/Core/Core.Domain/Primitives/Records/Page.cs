namespace Core.Domain.Primitives.Records;

public record Page
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalPages { get; init; } = 1;
    public int TotalCount { get; init; } = 0;

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}