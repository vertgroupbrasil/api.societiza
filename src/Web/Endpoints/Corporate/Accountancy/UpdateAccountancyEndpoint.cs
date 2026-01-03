using Ardalis.ApiEndpoints;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Corporate.Shared.Commands.Accountancy;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Attributes;
using Web.Endpoints.Corporate.Accountancy.Requests;
using Web.Endpoints.Corporate.Routes;

namespace Web.Endpoints.Corporate.Accountancy;

public class UpdateAccountancyEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<UpdateAccountancyRequest>
    .WithActionResult
{
    [NoTenantRequired]
    [HttpPut(AccountancyRoutes.UpdateAccountancy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [SwaggerOperation(
        Summary = "Update an accountancy",
        Description = @"Updates an existing accountancy and its associated tenant configuration.
        
        This endpoint allows modification of:
        - Accountancy registration details (legal name, trade name, address, contacts)
        - CNPJ (validated for uniqueness if changed)
        - Tenant expiration date
        - Database configuration (provider and connection string)
        
        **Behavior:**
        - Returns 404 Not Found if accountancy doesn't exist
        - Validates CNPJ uniqueness if changed
        - Updates both accountancy entity and related tenant data
        - Returns 204 No Content on success (no response body)
        
        **Validation:**
        - All required fields must be provided (legal name, CNPJ, address, contacts)
        - CNPJ must be valid Brazilian format
        - Email must be valid format
        - State must be valid 2-letter code
        
        **Use cases:**
        - Updating company information after changes
        - Correcting registration data
        - Extending or modifying expiration dates
        - Updating database configuration
        - Changing contact information",
        Tags = new[] { Tags.Accountancy })]
    public override async Task<ActionResult> HandleAsync(
        UpdateAccountancyRequest request,
        CancellationToken cancellationToken = default) =>
        await Result.Create(request)
            .Map(dto => new UpdateAccountancyCommand(
                dto.Id,
                dto.Body.LegalName,
                dto.Body.CNPJ,
                dto.Body.Address,
                dto.Body.City,
                dto.Body.State,
                dto.Body.PostalCode,
                dto.Body.Phone,
                dto.Body.Email,
                dto.Body.ExpirationDate,
                dto.Body.TradeName,
                dto.Body.Notes,
                dto.Body.DatabaseProvider,
                dto.Body.DatabaseConnectionString))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(NoContent, this.HandleFailure);
}