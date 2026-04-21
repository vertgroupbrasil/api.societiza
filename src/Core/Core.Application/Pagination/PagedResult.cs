using Core.Domain.Pagination;
using Core.Domain.Pagination.Interfaces;

namespace Core.Application.Pagination;

public record PagedResult<TItem> : IPagedResult<TItem> where TItem : class
{
    private readonly bool _hasNextPage;
    private readonly Paging _paging;

    private PagedResult(IReadOnlyCollection<TItem> items, Paging paging, bool hasNextPage)
    {
        Items = items;
        _paging = paging;
        _hasNextPage = hasNextPage;
    }

    public IReadOnlyCollection<TItem> Items { get; }

    public Page Page => new()
    {
        PageNumber = _paging.Number,
        PageSize = _paging.Size,
        HasNextPage = _hasNextPage,
        HasPreviousPage = _paging.Number > 1
    };

    public static IPagedResult<TItem> Create(Paging paging, IQueryable<TItem> source)
    {
        var items = ApplyPagination(paging, source).ToList();
        var hasNextPage = items.Count > paging.Size;

        if (hasNextPage)
            items = items.Take(paging.Size).ToList();

        return new PagedResult<TItem>(items, paging, hasNextPage);
    }

    private static IQueryable<TItem> ApplyPagination(Paging paging, IQueryable<TItem> source)
        => source.Skip(paging.Size * (paging.Number - 1)).Take(paging.Size + 1);
}