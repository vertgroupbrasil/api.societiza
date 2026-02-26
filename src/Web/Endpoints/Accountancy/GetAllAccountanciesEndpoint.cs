using Accountancy.Shared.Queries;
using Accountancy.Shared.Response;
using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Accountancy;

public sealed class GetAllAccountanciesEndpoint(ISender sender) : EndpointBaseAsync
    .WithoutRequest
    .WithActionResult<List<AccountancyDetailResponse>>
{
    [ApiVersion("1.0")]
    [HttpGet(AccountancyRoutes.GetAllAccountancies)]
    [ProducesResponseType(typeof(List<AccountancyDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(
            Summary = "Listar todas as contabilidades",
            Description = "Retorna a lista completa de contabilidades ativas, ordenadas alfabeticamente pela Razão Social (LegalName) e pela data de criação (CreatedAt).",
            Tags = [Tags.Accountancy])]
    public override async Task<ActionResult<List<AccountancyDetailResponse>>> HandleAsync(CancellationToken cancellationToken = default)
        => await Result.Create(new GetAllAccountanciesQuery())
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
}