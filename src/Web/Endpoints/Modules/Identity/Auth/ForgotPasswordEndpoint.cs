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

public sealed class ForgotPasswordEndpoint(ISender sender)
    : EndpointBaseAsync
        .WithRequest<ForgotPasswordRequest>
        .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpPost(IdentityRoutes.ForgotPassword)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(
        OperationId = "ForgotPassword",
        Summary     = "Solicitar redefinição de senha",
        Description = """
            Dispara o e-mail de redefinição de senha para o endereço informado.

            **Regras:**
            - Sempre retorna `200` independente do e-mail existir ou não (evita enumeração de contas).
            - Após receber o e-mail, o usuário acessa o link e chama `POST /reset-password` com o token recebido.

            **Respostas:**
            - `200` - solicitação aceita.
            - `400` - e-mail ausente ou formato inválido.

            **Efeitos:** E-mail de redefinição enviado de forma assíncrona quando o e-mail estiver cadastrado.

            **Autorização:** Público.
            """,
        Tags = [Tags.Identity.Auth])]
    public override async Task<ActionResult> HandleAsync(
        ForgotPasswordRequest request, CancellationToken cancellationToken)
        => await Result.Create(request)
            .Map(r => new ForgotPasswordCommand(Email: r.Email))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(() => Ok(), this.HandleFailure);
}