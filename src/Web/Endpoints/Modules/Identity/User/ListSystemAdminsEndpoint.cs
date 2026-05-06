using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Domain.Pagination.Interfaces;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Application.Policies;
using Identity.Shared.Queries.User;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Identity.User.Requests.Queries;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Identity.User;

[Authorize(Policy = IdentityPolicies.CanManageAccountancies)]
public sealed class ListSystemAdminsEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<ListSystemAdminsRequest>
    .WithActionResult<IPagedResult<object>>
{
    [ApiVersion("1.0")]
    [HttpGet(IdentityRoutes.ListSystemAdmins)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        OperationId = "ListSystemAdmins",
        Summary     = "Listar administradores do sistema",
        Description = """
            Retorna a lista paginada de SystemAdmins, ordenados por data de criacao decrescente.

            **Regras:**
            - `OnlyActive=true` (padrao) retorna apenas usuarios ativos (nao deletados).
            - `OnlyActive=false` inclui usuarios deletados.
            - `pageNumber` deve ser maior ou igual a 1.
            - `pageSize` deve estar entre 1 e 100.
            - `items` contem ate `pageSize` registros e `page` traz metadados de paginacao (`hasNextPage`, `hasPreviousPage`).

            **Valores de enumeração na resposta:**
            - `role`: `SystemAdmin`

            **Respostas:**
            - `200` - pagina de resultados.
            - `400` - parametros de filtro ou paginacao invalidos.
            - `401` - access token ausente ou invalido.
            - `403` - usuario sem permissao.

            **Efeitos:** Nenhum.

            **Autorizacao:** SystemAdmin.
            """,
        Tags = [Tags.Identity.Users])]
    public override async Task<ActionResult<IPagedResult<object>>> HandleAsync(
        ListSystemAdminsRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new ListSystemAdminsQuery(r.OnlyActive, r.PageNumber, r.PageSize))
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
