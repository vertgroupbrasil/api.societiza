using Accountancy.Shared.Queries;
using Accountancy.Shared.Response;
using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Accountancy.Requests.Queries;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Accountancy;

public class GetAccountancyDetailByIdEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<GetAccountancyDetailByIdRequest>
    .WithActionResult<AccountancyDetailResponse>
{
    [ApiVersion("1.0")]
    [HttpGet(AccountancyRoutes.GetAccountancyById)]
    [ProducesResponseType(typeof(AccountancyDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(
            Summary = "Obter detalhes de uma contabilidade por ID",
            Description = "Retorna os dados completos de uma contabilidade específica a partir do seu identificador único (GUID). Retorna 404 caso a contabilidade não seja encontrada.",
            Tags = [Tags.Accountancy])]
    public override async Task<ActionResult<AccountancyDetailResponse>> HandleAsync(
        GetAccountancyDetailByIdRequest request, 
        CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new GetAccountancyDetailByIdQuery(request.Id))
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
}