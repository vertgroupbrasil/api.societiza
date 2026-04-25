using Microsoft.AspNetCore.Mvc;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Identity.User.Requests.Commands;

public sealed class DeleteUserRequest
{
    [FromRoute(Name = IdentityRoutes.UserId)]
    public Guid UserId { get; set; }

    [FromQuery]
    public Guid? AccountancyId { get; set; }
}
