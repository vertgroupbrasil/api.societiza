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

public sealed class ResendInviteEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<ResendInviteRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpPost(IdentityRoutes.ResendInvite)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [SwaggerOperation(
        OperationId = "ResendInvite",
        Summary     = "Reenviar convite",
        Description = """
            Gera um novo token e reenvia o e-mail de convite pendente.

            **Regras:** Retorna 404 caso o convite não exista. Retorna 409 quando o convite não está no status `Pending` (já foi aceito ou revogado).
            **Efeitos:** Invalida o token anterior, gera um novo e dispara o reenvio do e-mail.
            **Autorização:** Pendente de aplicação (hoje público). Regra planejada: SystemAdmin ou AccountingAdmin.
            """,
        Tags = [Tags.Identity.Invites])]
    public override async Task<ActionResult> HandleAsync(ResendInviteRequest request, CancellationToken cancellationToken)
        => await Result.Create(request)
            .Map(request => new ResendInviteCommand(
                InviteId: request.InviteId))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(() => Ok(), this.HandleFailure);
}
