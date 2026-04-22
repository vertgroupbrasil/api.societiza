using Core.Application.Messaging.Queries;
using Identity.Shared.Response.Invitation;

namespace Identity.Shared.Queries.Invitation;

public sealed record GetInvitationLinkByIdQuery(Guid InvitationLinkId) : IQuery<InvitationLinkByIdResponse>;