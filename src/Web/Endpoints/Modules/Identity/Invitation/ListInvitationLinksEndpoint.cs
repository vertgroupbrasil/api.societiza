using Ardalis.ApiEndpoints;
using Asp.Versioning;
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
    .WithActionResult<List<InvitationLinkDetailResponse>>
{
    [ApiVersion("1.0")]
    [HttpGet(IdentityRoutes.ListInvitationLinks)]
    [ProducesResponseType(typeof(List<InvitationLinkDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        OperationId = "ListInvitationLinks",
        Summary     = "Listar links de convite",
        Description = """
            Retorna os links de convite visíveis para o usuário autenticado, ordenados do mais recente para o mais antigo.

            **Regras:**
            - AccountingAdmin visualiza apenas links do próprio tenant.
            - SystemAdmin sem AccountancyId visualiza links SA→SA; com AccountancyId visualiza links do tenant daquela contabilidade.
            - OnlyActive=true (padrão) filtra apenas links ativos e não expirados.

            **Efeitos:** Nenhum.

            **Autorização:** SystemAdmin ou AccountingAdmin.
            """,
        Tags = [Tags.Identity.Invites])]
    public override async Task<ActionResult<List<InvitationLinkDetailResponse>>> HandleAsync(
        ListInvitationLinksRequest request,
        CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new ListInvitationLinksQuery(r.OnlyActive, r.AccountancyId))
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
}