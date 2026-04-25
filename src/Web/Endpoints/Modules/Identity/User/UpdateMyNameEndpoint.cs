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
public sealed class UpdateMyNameEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<UpdateMyNameRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpPut(IdentityRoutes.UpdateMyName)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "UpdateMyName",
        Summary     = "Atualizar nome do próprio usuário",
        Description = """
            Atualiza o nome e sobrenome do usuário autenticado.

            **Regras:**
            - Cada campo aceita no máximo 80 caracteres.
            - Apenas o próprio usuário pode editar seu nome — não há como editar o nome de outro usuário por este endpoint.

            **Efeitos:** Nenhum.

            **Autorização:** Qualquer usuário autenticado.
            """,
        Tags = [Tags.Identity.Users])]
    public override async Task<ActionResult> HandleAsync(
        UpdateMyNameRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new UpdateUserNameCommand(r.FirstName, r.LastName))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(() => Ok(), this.HandleFailure);
}
