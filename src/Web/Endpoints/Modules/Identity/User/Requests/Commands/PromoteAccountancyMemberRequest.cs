using Microsoft.AspNetCore.Mvc;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Identity.User.Requests.Commands;

public sealed class PromoteAccountancyMemberRequest
{
    [FromRoute(Name = IdentityRoutes.AccountancyIdParam)]
    public Guid AccountancyId { get; set; }

    [FromRoute(Name = IdentityRoutes.UserId)]
    public Guid UserId { get; set; }
}
