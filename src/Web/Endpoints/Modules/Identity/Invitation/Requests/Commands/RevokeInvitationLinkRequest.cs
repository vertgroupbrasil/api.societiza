using Microsoft.AspNetCore.Mvc;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Identity.Invitation.Requests.Commands;

public sealed class RevokeInvitationLinkRequest
{
    [FromRoute(Name = IdentityRoutes.InvitationLinkId)]
    public Guid InvitationLinkId { get; set; }
}