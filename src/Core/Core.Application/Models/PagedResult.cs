using Core.Domain.Primitives.Interfaces;
using Core.Domain.Primitives.Records;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Models;

public class PagedResult<T> : IPagedResult<T>
{
    public IReadOnlyCollection<T> Items { get; private set; }
    public Page Page { get; private set; }

    public PagedResult(List<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        int totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);

        Page = new Page
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            TotalCount = totalCount
        };
    }

    public static async Task<PagedResult<T>> CreateAsync(IQueryable<T> source, int pageNumber, int pageSize)
    {
        var count = await source.CountAsync();
        var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<T>(items, count, pageNumber, pageSize);
    }

    public static PagedResult<T> Create(
        IQueryable<T> source, int pageNumber, int pageSize)
    {
        var count = source.Count();
        var items = source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<T>(items, count, pageNumber, pageSize);
    }
}