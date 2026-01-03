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

public class DeleteAccountancyEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<DeleteAccountancyRequest>
    .WithActionResult
{
    [NoTenantRequired]
    [HttpDelete(AccountancyRoutes.DeleteAccountancy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [SwaggerOperation(
        Summary = "Delete an accountancy",
        Description = @"Soft deletes an accountancy and its associated tenant (logical deletion).
        
        This endpoint performs a soft delete, marking records as deleted without physically removing them from the database.
        
        **Behavior:**
        - Returns 404 Not Found if accountancy doesn't exist
        - Marks accountancy as deleted (sets IsDeleted = true, DeletedAt timestamp)
        - Marks associated tenant as deleted
        - Preserves all data for audit and recovery purposes
        - Returns 204 No Content on success (no response body)
        
        **Cascade effects:**
        - Tenant is soft deleted
        - Associated modules remain but become inaccessible
        - Historical data is preserved for compliance
        
        **Important notes:**
        - This is a logical deletion (soft delete)
        - Data can be recovered by system administrators
        - CNPJ becomes available for re-use after deletion
        - Deleted accountancies won't appear in regular queries
        
        **Use cases:**
        - Client termination or cancellation
        - Company closure
        - Data archival with preservation
        - Compliance with data retention policies",
        Tags = new[] { Tags.Accountancy })]
    public override async Task<ActionResult> HandleAsync(
        DeleteAccountancyRequest request,
        CancellationToken cancellationToken = default) =>
        await Result.Create(request)
            .Map(dto => new DeleteAccountancyCommand(dto.Id))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(NoContent, this.HandleFailure);
}