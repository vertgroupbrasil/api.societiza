using Core.Domain.Pagination;
using Core.Domain.Pagination.Interfaces;

namespace Core.Application.Pagination;

public record PagedResult<TItem> : IPagedResult<TItem> where TItem : class
{
    private readonly int _totalCount;
    private readonly Paging _paging;

    private PagedResult(IReadOnlyCollection<TItem> items, Paging paging, int totalCount)
    {
        Items = items;
        _paging = paging;
        _totalCount = totalCount;
    }

    public IReadOnlyCollection<TItem> Items { get; }

    public Page Page
    {
        get
        {
            var totalPages = _paging.Size > 0
                ? (int)Math.Ceiling((double)_totalCount / _paging.Size)
                : 0;

            return new()
            {
                PageNumber = _paging.Number,
                PageSize = _paging.Size,
                HasNextPage = _paging.Number < totalPages,
                HasPreviousPage = _paging.Number > 1,
                TotalCount = _totalCount,
                TotalPages = totalPages
            };
        }
    }

    public static IPagedResult<TItem> Create(Paging paging, IQueryable<TItem> source)
    {
        var totalCount = source.Count();
        var items = source
            .Skip(paging.Size * (paging.Number - 1))
            .Take(paging.Size)
            .ToList();

        return new PagedResult<TItem>(items, paging, totalCount);
    }

    public static IPagedResult<TItem> Create(Paging paging, IQueryable<TItem> source, int totalCount)
    {
        var items = source
            .Skip(paging.Size * (paging.Number - 1))
            .Take(paging.Size)
            .ToList();

        return new PagedResult<TItem>(items, paging, totalCount);
    }
}