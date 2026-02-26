using Microsoft.AspNetCore.Mvc;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Accountancy.Requests.Queries;

public class GetAccountancyDetailByIdRequest
{
    [FromRoute(Name = AccountancyRoutes.AccountancyId)]
    public Guid Id { get; set; }
}