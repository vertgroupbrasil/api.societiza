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

public sealed class AcceptInviteEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<AcceptInviteRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpPost(IdentityRoutes.AcceptInvite)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [SwaggerOperation(
        OperationId = "AcceptInvite",
        Summary     = "Aceitar convite",
        Description = """
            Consome o token de convite, define a senha do usuário e cria a conta.

            **Regras:** Retorna 400 quando o token é inválido ou expirado, as senhas não coincidem ou a senha não atende os requisitos mínimos. Retorna 404 quando o convite não existe. Retorna 409 quando o convite já foi aceito ou revogado.
            **Efeitos:** Cria o usuário e marca o convite como aceito.
            **Autorização:** Público (prova de posse via token).
            """,
        Tags = [Tags.Identity.Invites])]
    public override async Task<ActionResult> HandleAsync(AcceptInviteRequest request, CancellationToken cancellationToken)
        => await Result.Create(request)
            .Map(request => new AcceptInviteCommand(
                Token: request.Token,
                FirstName: request.FirstName,
                LastName: request.LastName,
                Password: request.Password,
                ConfirmPassword: request.ConfirmPassword))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(() => Ok(), this.HandleFailure);
}
