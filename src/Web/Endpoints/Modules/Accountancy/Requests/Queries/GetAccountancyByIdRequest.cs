using Microsoft.AspNetCore.Mvc;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Accountancy.Requests.Queries;

public class GetAccountancyByIdRequest
{
    [FromRoute(Name = AccountancyRoutes.AccountancyId)]
    public Guid Id { get; set; }
}
