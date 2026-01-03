using Microsoft.AspNetCore.Mvc;
using Web.Endpoints.Corporate.Routes;

namespace Web.Endpoints.Corporate.Accountancy.Requests;

public class GetAccountancyByIdRequest
{
    [FromRoute(Name = AccountancyRoutes.AccountancyId)]
    public Guid Id { get; set; }
}