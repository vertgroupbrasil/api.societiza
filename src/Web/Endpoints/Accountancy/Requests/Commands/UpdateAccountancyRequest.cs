using Microsoft.AspNetCore.Mvc;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Accountancy.Requests.Commands;

public class UpdateAccountancyRequest
{
    [FromRoute(Name = AccountancyRoutes.AccountancyId)]
    public Guid Id { get; set; }

    [FromBody]
    public UpdateAccountancyBody Body { get; set; } = null!;
}

public class UpdateAccountancyBody
{
    public string CNPJ { get; set; } = null!;
    public string LegalName { get; set; } = null!;
    public string? TradeName { get; set; }
    public string Address { get; set; } = null!;
    public string City { get; set; } = null!;
    public string State { get; set; } = null!;
    public string PostalCode { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Email { get; set; }
}