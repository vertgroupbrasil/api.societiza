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
            Inicia o fluxo de redefinição de senha para o e-mail informado.

            **Regras:** Sempre retorna 200, independentemente do e-mail existir, para evitar enumeração de contas.
            **Efeitos:** Se o usuário existir, envia um e-mail com o link de redefinição contendo um token com validade limitada.
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
