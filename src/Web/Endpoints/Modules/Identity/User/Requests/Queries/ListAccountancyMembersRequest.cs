using Microsoft.AspNetCore.Mvc;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Identity.User.Requests.Queries;

public sealed class ListAccountancyMembersRequest
{
    [FromRoute(Name = IdentityRoutes.AccountancyIdParam)]
    public Guid AccountancyId { get; set; }
}
