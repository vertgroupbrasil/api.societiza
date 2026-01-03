using Ardalis.ApiEndpoints;
using Core.Application.Models;
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

public sealed class GetAccountanciesEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<GetAccountanciesRequest>
    .WithActionResult<PagedResult<AccountancyListResponse>>
{
    [NoTenantRequired]
    [HttpGet(AccountancyRoutes.GetAccountancies)]
    [ProducesResponseType(typeof(PagedResult<AccountancyListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(
        Summary = "List accountancies with filters and pagination",
        Description = @"Returns a filtered and paginated list of accountancies (accounting firms).
        
        **Available Filters:**
        - **searchTerm**: Search in legal name, trade name, CNPJ, email, and tenant name (case insensitive)
        - **state**: Filter by state/UF (e.g., SP, RJ, MG) - case insensitive
        - **city**: Filter by city - case insensitive
        - **includeDeleted**: Include soft-deleted accountancies - default: false
        - **includeExpired**: Include expired accountancies (based on tenant expiration date) - default: false
        - **hasDedicatedDatabase**: Filter by infrastructure type (true/false/null)
        
        **Pagination:**
        - **pageNumber**: Page number (starts at 1) - default: 1
        - **pageSize**: Items per page (max: 100) - default: 50
        
        **Default Behavior:**
        - Returns only active (non-deleted, non-expired) accountancies
        - Ordered by legal name, then by city
        
        **Response Structure:**
        - **items**: List of accountancies for the current page
        - **page**: Pagination metadata (pageNumber, pageSize, totalPages, totalCount, hasPreviousPage, hasNextPage)
        
        **Examples:**
        ```
        GET /corporate/accountancy?searchTerm=Contabilidade&pageSize=20
        GET /corporate/accountancy?state=SP&city=São Paulo&pageNumber=2
        GET /corporate/accountancy?includeExpired=true&includeDeleted=false
        GET /corporate/accountancy?hasDedicatedDatabase=true
        ```",
        Tags = new[] { Tags.Accountancy })]
    public override async Task<ActionResult<PagedResult<AccountancyListResponse>>> HandleAsync(
        [FromQuery] GetAccountanciesRequest request,
        CancellationToken cancellationToken = default) =>
        await Result.Create(request)
            .Map(dto => new GetAccountanciesQuery
            {
                SearchTerm = request.SearchTerm,
                State = request.State,
                City = request.City,
                IncludeDeleted = request.IncludeDeleted,
                IncludeExpired = request.IncludeExpired,
                HasDedicatedDatabase = request.HasDedicatedDatabase,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            })
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
}