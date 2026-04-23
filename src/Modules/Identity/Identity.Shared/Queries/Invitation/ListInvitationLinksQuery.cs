using Core.Application.Messaging.Queries;
using Core.Domain.Pagination.Interfaces;
using Identity.Shared.Response.Invitation;

namespace Identity.Shared.Queries.Invitation;

public sealed record ListInvitationLinksQuery(
    bool OnlyActive,
    Guid? AccountancyId,
    int PageNumber,
    int PageSize
) : IQuery<IPagedResult<InvitationLinkDetailResponse>>;