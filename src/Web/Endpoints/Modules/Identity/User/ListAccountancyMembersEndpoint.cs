using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Domain.Pagination.Interfaces;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Shared.Queries.User;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Identity.User.Requests.Queries;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Identity.User;

[Authorize]
public sealed class ListAccountancyMembersEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<ListAccountancyMembersRequest>
    .WithActionResult<IPagedResult<object>>
{
    [ApiVersion("1.0")]
    [HttpGet(IdentityRoutes.ListAccountancyMembers)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        OperationId = "ListAccountancyMembers",
        Summary     = "Listar membros da contabilidade",
        Description = """
            Retorna a lista paginada de membros de uma contabilidade. O nivel de detalhe varia conforme o perfil do solicitante.

            **Regras:**
            - AccountancyAdmin e SystemAdmin recebem dados completos (e-mail, data de entrada, link de convite).
            - AccountancyEmployee recebe apenas nome e perfil dos colegas.
            - AccountancyAdmin e AccountancyEmployee so podem listar membros da propria contabilidade.
            - SystemAdmin pode listar membros de qualquer contabilidade.
            - `OnlyActive=true` (padrao) retorna apenas usuarios ativos (nao deletados).
            - `OnlyActive=false` inclui usuarios deletados.
            - `pageNumber` deve ser maior ou igual a 1.
            - `pageSize` deve estar entre 1 e 100.
            - `items` contem ate `pageSize` registros e `page` traz metadados de paginacao (`hasNextPage`, `hasPreviousPage`).

            **Valores de enumeração na resposta:**
            - `role`: `SystemAdmin` | `AccountancyAdmin` | `AccountancyEmployee`

            **Respostas:**
            - `200` - pagina de resultados, com detalhes filtrados por perfil.
            - `400` - identificador, filtro ou parametros de paginacao invalidos.
            - `401` - access token ausente ou invalido.
            - `403` - usuario sem permissao.

            **Efeitos:** Nenhum.

            **Autorizacao:** Qualquer usuario autenticado (dados filtrados por perfil).
            """,
        Tags = [Tags.Identity.Users])]
    public override async Task<ActionResult<IPagedResult<object>>> HandleAsync(
        ListAccountancyMembersRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new ListAccountancyMembersQuery(r.AccountancyId, r.OnlyActive, r.PageNumber, r.PageSize))
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
