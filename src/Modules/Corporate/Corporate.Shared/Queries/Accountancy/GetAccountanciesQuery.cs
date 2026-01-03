using Core.Application.Messaging.Queries;
using Core.Application.Models;
using Corporate.Shared.Response.Accountancy;

namespace Corporate.Shared.Queries.Accountancy;

public sealed record GetAccountanciesQuery : IQuery<PagedResult<AccountancyListResponse>>
{
    public string? SearchTerm { get; init; }

    public string? State { get; init; }
    public string? City { get; init; }

    public bool IncludeDeleted { get; init; } = false;
    public bool IncludeExpired { get; init; } = false;

    public bool? HasDedicatedDatabase { get; init; }

    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}