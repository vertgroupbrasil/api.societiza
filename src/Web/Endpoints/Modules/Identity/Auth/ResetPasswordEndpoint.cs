using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Shared.Commands.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Identity.Auth.Requests;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Identity.Auth;

public sealed class ResetPasswordEndpoint(ISender sender)
    : EndpointBaseAsync
        .WithRequest<ResetPasswordRequest>
        .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpPost(IdentityRoutes.ResetPassword)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "ResetPassword",
        Summary     = "Redefinir senha",
        Description = """
            Consome o token de redefinição e define a nova senha do usuário.

            **Respostas:**
            - `200` — senha alterada; o token é invalidado e não pode ser reutilizado.
            - `400` — token inválido, expirado ou já utilizado; ou nova senha fora dos requisitos mínimos.
            - `404` — usuário associado ao token não encontrado.

            **Requisitos de senha:** mínimo 8 caracteres, ao menos uma letra maiúscula, uma minúscula e um dígito.
            """,
        Tags = [Tags.Identity.Auth])]
    public override async Task<ActionResult> HandleAsync(
        ResetPasswordRequest request, CancellationToken cancellationToken)
        => await Result.Create(request)
            .Map(r => new ResetPasswordCommand(Token: r.Token, NewPassword: r.NewPassword, ConfirmPassword: r.ConfirmPassword))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(() => Ok(), this.HandleFailure);
}