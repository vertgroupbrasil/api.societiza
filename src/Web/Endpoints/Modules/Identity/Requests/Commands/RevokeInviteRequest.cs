using Microsoft.AspNetCore.Mvc;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Identity.Requests.Commands;

public sealed class RevokeInviteRequest
{
    [FromRoute(Name = IdentityRoutes.InviteId)]
    public Guid InviteId { get; set; }
}