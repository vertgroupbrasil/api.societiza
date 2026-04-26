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

            **Regras:**
            - Token é single-use: após o consumo, não pode ser reutilizado.
            - Nova senha deve ter no mínimo 8 caracteres, ao menos uma letra maiúscula, uma minúscula e um dígito.
            - `ConfirmPassword` deve ser igual a `NewPassword`.

            **Respostas:**
            - `200` - senha alterada com sucesso.
            - `400` - token inválido, expirado ou já utilizado, ou nova senha fora dos requisitos.
            - `404` - usuário associado ao token não encontrado.

            **Efeitos:** Senha do usuário atualizada; token de redefinição invalidado.

            **Autorização:** Público.
            """,
        Tags = [Tags.Identity.Auth])]
    public override async Task<ActionResult> HandleAsync(
        ResetPasswordRequest request, CancellationToken cancellationToken)
        => await Result.Create(request)
            .Map(r => new ResetPasswordCommand(Token: r.Token, NewPassword: r.NewPassword, ConfirmPassword: r.ConfirmPassword))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(() => Ok(), this.HandleFailure);
}