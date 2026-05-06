using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Shared.Queries.User;
using Identity.Shared.Response.User;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Identity.User;

[Authorize]
public sealed class GetMyProfileEndpoint(ISender sender) : EndpointBaseAsync
    .WithoutRequest
    .WithActionResult<UserProfileResponse>
{
    [ApiVersion("1.0")]
    [HttpGet(IdentityRoutes.GetMyProfile)]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "GetMyProfile",
        Summary     = "Obter perfil do próprio usuário",
        Description = """
            Retorna os dados do perfil do usuário autenticado.

            **Regras:**
            - Retorna os dados do próprio usuário — não é possível consultar outros usuários por este endpoint.

            **Valores de enumeração na resposta:**
            - `role`: `SystemAdmin` | `AccountancyAdmin` | `AccountancyEmployee`

            **Respostas:**
            - `200` - perfil do usuário autenticado.
            - `400` - requisição inválida.
            - `401` - access token ausente ou inválido.
            - `403` - usuário sem permissão.
            - `404` - usuário não encontrado.

            **Efeitos:** Nenhum.

            **Autorização:** Qualquer usuário autenticado.
            """,
        Tags = [Tags.Identity.Users])]
    public override async Task<ActionResult<UserProfileResponse>> HandleAsync(
        CancellationToken cancellationToken = default)
        => await Result.Create(new GetMyProfileQuery())
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
