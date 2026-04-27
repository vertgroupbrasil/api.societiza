using Core.Application.Messaging.Queries;
using Core.Domain.Pagination.Interfaces;
using Identity.Shared.Response.User;

namespace Identity.Shared.Queries.User;

public sealed record ListSystemAdminsQuery(
    bool OnlyActive,
    int PageNumber,
    int PageSize
) : IQuery<IPagedResult<UserDetailResponse>>;
