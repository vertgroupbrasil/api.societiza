using Microsoft.AspNetCore.Mvc;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Identity.User.Requests.Queries;

public sealed class GetUserByIdRequest
{
    [FromRoute(Name = IdentityRoutes.UserId)]
    public Guid UserId { get; set; }
}
