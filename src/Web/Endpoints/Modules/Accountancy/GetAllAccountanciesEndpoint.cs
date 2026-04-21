using Accountancy.Shared.Queries;
using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Domain.Pagination.Interfaces;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Application.Policies;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Accountancy.Requests.Queries;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Accountancy;

[Authorize(Policy = IdentityPolicies.CanManageAccountancies)]
public sealed class GetAllAccountanciesEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<GetAllAccountanciesRequest>
    .WithActionResult<IPagedResult<object>>
{
    [ApiVersion("1.0")]
    [HttpGet(AccountancyRoutes.GetAllAccountancies)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        OperationId = "GetAllAccountancies",
        Summary     = "Listar contabilidades",
        Description = """
            Retorna a lista paginada de contabilidades ativas, ordenadas por Razão Social (`LegalName`) e data de criação (`CreatedAt`).

            **Respostas:**
            - `200` — página de resultados; `items` contém até `pageSize` registros e `page` traz metadados de paginação (`hasNextPage`, `hasPreviousPage`).
            - `400` — parâmetros de paginação inválidos (`pageNumber` < 1 ou `pageSize` fora de 1–100).
            - `401` — access token ausente ou inválido.
            - `403` — usuário sem permissão (requer SystemAdmin).
            """,
        Tags = [Tags.Accountancy])]
    public override async Task<ActionResult<IPagedResult<object>>> HandleAsync(GetAllAccountanciesRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new GetAllAccountanciesQuery(r.PageNumber, r.PageSize))
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
}