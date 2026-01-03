using Ardalis.ApiEndpoints;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Corporate.Shared.Queries.Accountancy;
using Corporate.Shared.Response.Accountancy;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Attributes;
using Web.Endpoints.Corporate.Accountancy.Requests;
using Web.Endpoints.Corporate.Routes;

namespace Web.Endpoints.Corporate.Accountancy;

public sealed class GetAccountancyByIdEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<GetAccountancyByIdRequest>
    .WithActionResult<AccountancyDetailResponse>
{
    [NoTenantRequired]
    [HttpGet(AccountancyRoutes.GetAccountancyById)]
    [ProducesResponseType(typeof(AccountancyDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [SwaggerOperation(
        Summary = "Get accountancy by ID",
        Description = @"Retrieves detailed information about a specific accountancy including tenant and module data.
        
        This endpoint returns comprehensive accountancy details with:
        - Complete registration information (legal name, CNPJ, address, contacts)
        - Associated tenant summary with computed properties
        - List of contracted modules with usage statistics
        - Module expiration and status information
        
        **Response includes:**
        - Accountancy details: Legal name, trade name, CNPJ, address, contacts
        - Tenant information:
          - Basic data: ID, identifier, name, database configuration
          - Computed properties: IsExpired, DaysUntilExpiration, IsExpiringSoon
          - Statistics: ActiveModulesCount, ExpiredModulesCount, DaysActive
        - Modules array with:
          - Module configuration and status (enabled/disabled)
          - Usage metrics with percentages (users, records, storage)
          - Expiration dates and restrictions
        
        **Use cases:**
        - Viewing complete accountancy profile
        - Monitoring tenant health and module usage
        - Checking license expiration and limits
        - Auditing module configurations",
        Tags = new[] { Tags.Accountancy })]
    public override async Task<ActionResult<AccountancyDetailResponse>> HandleAsync(
        GetAccountancyByIdRequest request,
        CancellationToken cancellationToken = default) =>
        await Result.Create(request)
            .Map(dto => new GetAccountancyByIdQuery(dto.Id))
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
}