using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Shared.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Identity.Requests.Commands;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Identity;

public sealed class RevokeInviteEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<RevokeInviteRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpPost(IdentityRoutes.RevokeInvite)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [SwaggerOperation(
        OperationId = "RevokeInvite",
        Summary     = "Revogar convite",
        Description = """
            Revoga um convite pendente, impedindo seu aceite futuro.

            **Regras:** Retorna 404 caso o convite não exista. Retorna 409 quando o convite não está no status `Pending`.
            **Efeitos:** Marca o convite como revogado.
            **Autorização:** Pendente de aplicação (hoje público). Regra planejada: SystemAdmin ou AccountingAdmin.
            """,
        Tags = [Tags.Identity.Invites])]
    public override async Task<ActionResult> HandleAsync(RevokeInviteRequest request, CancellationToken cancellationToken)
        => await Result.Create(request)
            .Map(request => new RevokeInviteCommand(
                InviteId: request.InviteId))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(() => Ok(), this.HandleFailure);
}
