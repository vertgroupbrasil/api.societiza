using Core.Application.Messaging.Queries;
using Core.Domain.Pagination.Interfaces;

namespace Identity.Shared.Queries.User;

public sealed record ListAccountancyMembersQuery(
    Guid AccountancyId,
    bool OnlyActive,
    int PageNumber,
    int PageSize
) : IQuery<IPagedResult<object>>;
