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
using Web.Endpoints.Modules.Identity.User.Requests.Queries;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Identity.User;

[Authorize]
public sealed class GetMyProfileEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<GetMyProfileRequest>
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

            **Efeitos:** Nenhum.

            **Autorização:** Qualquer usuário autenticado.
            """,
        Tags = [Tags.Identity.Users])]
    public override async Task<ActionResult<UserProfileResponse>> HandleAsync(
        GetMyProfileRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(_ => new GetMyProfileQuery())
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
