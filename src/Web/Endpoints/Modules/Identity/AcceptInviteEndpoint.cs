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
        Summary = "Aceitar convite e concluir cadastro",
        Description = "Valida o token de convite, define a senha do usuário e ativa a conta. Retorna 400 caso o token seja inválido ou expirado, as senhas não coincidam, ou a senha não atenda os requisitos mínimos.",
        Tags = [Tags.Identity])]
    public override async Task<ActionResult> HandleAsync(AcceptInviteRequest request, CancellationToken cancellationToken)
        => await Result.Create(request)
            .Map(request => new AcceptInviteCommand(
                Token: request.Token,
                Password: request.Password,
                ConfirmPassword: request.ConfirmPassword))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(() => Ok(), this.HandleFailure);
}