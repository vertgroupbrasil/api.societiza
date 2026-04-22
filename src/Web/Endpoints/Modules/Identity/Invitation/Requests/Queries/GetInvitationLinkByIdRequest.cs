using Microsoft.AspNetCore.Mvc;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Identity.Invitation.Requests.Queries;

public sealed class GetInvitationLinkByIdRequest
{
    [FromRoute(Name = IdentityRoutes.InvitationLinkId)]
    public Guid InvitationLinkId { get; set; }
}