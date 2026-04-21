using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Application.Policies;
using Identity.Shared.Commands.Invitation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Identity.Invitation.Requests.Commands;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Identity.Invitation;

[Authorize(Policy = IdentityPolicies.CanManageInvites)]
public sealed class RevokeInvitationLinkEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<RevokeInvitationLinkRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpPost(IdentityRoutes.RevokeInvitationLink)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [SwaggerOperation(
        OperationId = "RevokeInvitationLink",
        Summary     = "Revogar link de convite",
        Description = """
            Revoga um link de convite ativo, tornando-o inutilizável imediatamente.

            **Regras:**
            - O link deve estar ativo — retorna 409 se já estiver inativo.
            - SystemAdmin pode revogar apenas links SA→SA.
            - AccountancyAdmin pode revogar links do próprio tenant (AA→AA, AA→AE, SA→AA).

            **Efeitos:** Nenhum.

            **Autorização:** SystemAdmin ou AccountancyAdmin.
            """,
        Tags = [Tags.Identity.Invites])]
    public override async Task<ActionResult> HandleAsync(
        RevokeInvitationLinkRequest request,
        CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new RevokeInvitationLinkCommand(r.InvitationLinkId))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(() => Ok(), this.HandleFailure);
}