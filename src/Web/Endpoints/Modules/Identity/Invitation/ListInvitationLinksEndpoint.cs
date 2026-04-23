using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Domain.Pagination.Interfaces;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Application.Policies;
using Identity.Shared.Queries.Invitation;
using Identity.Shared.Response.Invitation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Identity.Invitation.Requests.Queries;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Identity.Invitation;

[Authorize(Policy = IdentityPolicies.CanManageInvites)]
public sealed class ListInvitationLinksEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<ListInvitationLinksRequest>
    .WithActionResult<IPagedResult<object>>
{
    [ApiVersion("1.0")]
    [HttpGet(IdentityRoutes.ListInvitationLinks)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        OperationId = "ListInvitationLinks",
        Summary     = "Listar links de convite",
        Description = """
            Retorna a lista paginada de links de convite visíveis para o usuário autenticado, ordenados por status ativo e data de criação decrescente.

            **Regras:**
            - AccountancyAdmin visualiza apenas links do próprio tenant.
            - SystemAdmin sem AccountancyId visualiza links SA→SA; com AccountancyId visualiza links do tenant daquela contabilidade.
            - OnlyActive=true (padrão) filtra apenas links ativos e não expirados.

            **Respostas:**
            - `200` — página de resultados; `items` contém até `pageSize` registros e `page` traz metadados de paginação (`hasNextPage`, `hasPreviousPage`).
            - `400` — parâmetros de paginação inválidos (`pageNumber` < 1 ou `pageSize` fora de 1–100).
            - `401` — access token ausente ou inválido.
            - `403` — usuário sem permissão (requer SystemAdmin ou AccountancyAdmin).
            """,
        Tags = [Tags.Identity.Invites])]
    public override async Task<ActionResult<IPagedResult<object>>> HandleAsync(ListInvitationLinksRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new ListInvitationLinksQuery(r.OnlyActive, r.AccountancyId, r.PageNumber, r.PageSize))
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
}