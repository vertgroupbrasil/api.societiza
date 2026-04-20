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
public sealed class ListInvitationLinkUsersEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<ListInvitationLinkUsersRequest>
    .WithActionResult<List<InvitationLinkUserResponse>>
{
    [ApiVersion("1.0")]
    [HttpGet(IdentityRoutes.ListInvitationLinkUsers)]
    [ProducesResponseType(typeof(List<InvitationLinkUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "ListInvitationLinkUsers",
        Summary     = "Listar usuários de um link de convite",
        Description = """
            Retorna os usuários que se cadastraram através de um link de convite específico, ordenados por data de cadastro.

            **Regras:**
            - O link deve existir — retorna 404 caso contrário.
            - AccountingAdmin só pode consultar links do próprio tenant.
            - SystemAdmin tem acesso a qualquer link.

            **Efeitos:** Nenhum.

            **Autorização:** SystemAdmin ou AccountingAdmin.
            """,
        Tags = [Tags.Identity.Invites])]
    public override async Task<ActionResult<List<InvitationLinkUserResponse>>> HandleAsync(
        ListInvitationLinkUsersRequest request,
        CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new ListUsersByInvitationLinkQuery(r.InvitationLinkId))
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
}