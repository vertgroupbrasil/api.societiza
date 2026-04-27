using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Shared.Commands.User;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Identity.User.Requests.Commands;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Identity.User;

[Authorize]
public sealed class UpdateUserEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<UpdateUserRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpPut(IdentityRoutes.UpdateUser)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "UpdateUser",
        Summary     = "Atualizar usuario",
        Description = """
            Atualiza o nome e sobrenome do usuario autenticado.

            **Regras:**
            - Cada campo aceita no maximo 80 caracteres.
            - Apenas o proprio usuario pode editar seus dados por este endpoint.

            **Respostas:**
            - `200` - usuario atualizado com sucesso.
            - `400` - dados invalidos ou ausentes.
            - `401` - access token ausente ou invalido.
            - `403` - usuario sem permissao.
            - `404` - usuario nao encontrado.

            **Efeitos:** Dados do usuario autenticado atualizados.

            **Autorizacao:** Qualquer usuario autenticado.
            """,
        Tags = [Tags.Identity.Users])]
    public override async Task<ActionResult> HandleAsync(
        UpdateUserRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new UpdateUserCommand(r.FirstName, r.LastName))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(() => Ok(), this.HandleFailure);
}