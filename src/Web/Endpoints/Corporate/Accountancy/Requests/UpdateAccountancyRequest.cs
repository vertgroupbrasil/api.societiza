using Corporate.Domain.DTOs;
using Microsoft.AspNetCore.Mvc;
using Web.Endpoints.Corporate.Routes;

namespace Web.Endpoints.Corporate.Accountancy.Requests;

public class UpdateAccountancyRequest
{
    [FromRoute(Name = AccountancyRoutes.AccountancyId)]
    public Guid Id { get; set; }

    [FromBody]
    public AccountancyDto.UpdateAccountancy Body { get; set; } = new();
}