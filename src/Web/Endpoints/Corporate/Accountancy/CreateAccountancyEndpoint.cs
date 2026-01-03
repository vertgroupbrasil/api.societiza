using Ardalis.ApiEndpoints;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Corporate.Shared.Commands.Accountancy;
using Corporate.Shared.Response.Accountancy;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Attributes;
using Web.Endpoints.Corporate.Accountancy.Requests;
using Web.Endpoints.Corporate.Routes;

namespace Web.Endpoints.Corporate.Accountancy;

public sealed class CreateAccountancyEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<CreateAccountancyRequest>
    .WithActionResult<CreateAccountancyResponse>
{
    [NoTenantRequired]
    [HttpPost(AccountancyRoutes.CreateAccountancy)]
    [ProducesResponseType(typeof(CreateAccountancyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [SwaggerOperation(
        Summary = "Create a new accountancy",
        Description = @"Creates a new accountancy (accounting firm) with its own dedicated tenant.
        
        This endpoint initializes a complete multi-tenant structure including:
        - New accountancy entity with registration details (CNPJ, address, contacts)
        - Associated tenant with unique identifier
        - Optional dedicated database configuration
        
        **Behavior:**
        - Validates CNPJ uniqueness across the system
        - Creates tenant identifier in format: tenant-{cnpj}
        - Establishes expiration date (default: 1 year)
        - Optionally configures dedicated database connection
        
        **Response includes:**
        - AccountancyId: Unique identifier for the accounting firm
        - TenantId: Associated tenant identifier
        - TenantIdentifier: Human-readable tenant identifier (tenant-{cnpj})
        
        **Use cases:**
        - Onboarding new accounting firms
        - Setting up isolated tenant environments
        - Initializing multi-tenant structure",
        Tags = new[] { Tags.Accountancy })]
    public override async Task<ActionResult<CreateAccountancyResponse>> HandleAsync(
        CreateAccountancyRequest request, 
        CancellationToken cancellationToken = default) =>
        await Result.Create(request)
            .Map(dto => new CreateAccountancyCommand(
                dto.LegalName,
                dto.CNPJ,
                dto.Address,
                dto.City,
                dto.State,
                dto.PostalCode,
                dto.Phone,
                dto.Email,
                dto.TradeName,
                dto.Notes,
                dto.DatabaseProvider,
                dto.DatabaseConnectionString,
                dto.ExpirationDate))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}