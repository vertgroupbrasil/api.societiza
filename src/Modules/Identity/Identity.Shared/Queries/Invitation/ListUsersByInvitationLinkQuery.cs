using Core.Application.Messaging.Queries;
using Identity.Shared.Response.Invitation;

namespace Identity.Shared.Queries.Invitation;

public sealed record ListUsersByInvitationLinkQuery(
    Guid InvitationLinkId
) : IQuery<List<InvitationLinkUserResponse>>;
