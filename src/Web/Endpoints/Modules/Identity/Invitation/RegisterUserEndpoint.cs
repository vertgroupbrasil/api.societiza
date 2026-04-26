using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Response;
using Core.Shared.Results;
using Identity.Shared.Commands.User;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Identity.Invitation.Requests.Commands;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Identity.Invitation;

public sealed class RegisterUserEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<RegisterUserRequest>
    .WithActionResult<IdentifierResponse>
{
    [ApiVersion("1.0")]
    [HttpPost(IdentityRoutes.Register)]
    [ProducesResponseType(typeof(IdentifierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [SwaggerOperation(
        OperationId = "RegisterUser",
        Summary     = "Cadastrar usuário via link de convite",
        Description = """
            Cria uma nova conta a partir de um link de convite válido e retorna o identificador do usuário criado.

            **Regras:**
            - O token deve corresponder a um link ativo, não expirado e não esgotado — retorna 404 se o link não existir ou 400 com motivo específico se estiver inválido (Expired, Exhausted, Revoked).
            - Retorna 409 se já existir uma conta com o mesmo e-mail.
            - Senha deve ter no mínimo 8 caracteres.
            - `ConfirmPassword` deve ser igual a `Password`.

            **Respostas:**
            - `200` - usuário criado; retorna o identificador gerado.
            - `400` - dados inválidos ou link em estado inválido (Expired, Exhausted, Revoked).
            - `404` - link de convite não encontrado.
            - `409` - já existe uma conta com o mesmo e-mail.

            **Efeitos:** Novo usuário criado e vinculado ao link de convite.

            **Autorização:** Público.
            """,
        Tags = [Tags.Identity.Invites])]
    public override async Task<ActionResult<IdentifierResponse>> HandleAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new RegisterUserCommand(r.Token, r.FirstName, r.LastName, r.Email, r.Password, r.ConfirmPassword))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
