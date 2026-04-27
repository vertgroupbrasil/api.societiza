using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Application.Policies;
using Identity.Shared.Commands.User;
using Identity.Shared.Response.User;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Identity.User.Requests.Commands;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Identity.User;

[Authorize(Policy = IdentityPolicies.CanPromoteUser)]
public sealed class PromoteUserEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<PromoteUserRequest>
    .WithActionResult<UserDetailResponse>
{
    [ApiVersion("1.0")]
    [HttpPost(IdentityRoutes.PromoteUser)]
    [ProducesResponseType(typeof(UserDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [SwaggerOperation(
        OperationId = "PromoteUser",
        Summary     = "Promover usuario",
        Description = """
            Promove um AccountancyEmployee a AccountancyAdmin dentro da contabilidade indicada.

            **Regras:**
            - Apenas AccountancyAdmin pode promover usuarios; SystemAdmin nao tem acesso a esta acao.
            - O usuario promovido deve pertencer a mesma contabilidade do solicitante.
            - Somente AccountancyEmployee pode ser promovido; usuarios ja administradores retornam 409.

            **Respostas:**
            - `200` - usuario promovido; retorna os dados atualizados.
            - `400` - dados invalidos ou ausentes.
            - `401` - access token ausente ou invalido.
            - `403` - usuario sem permissao.
            - `404` - contabilidade ou usuario nao encontrado.
            - `409` - usuario ja e AccountancyAdmin.

            **Efeitos:** Perfil do usuario e atualizado para AccountancyAdmin imediatamente.

            **Autorizacao:** AccountancyAdmin.
            """,
        Tags = [Tags.Identity.Users])]
    public override async Task<ActionResult<UserDetailResponse>> HandleAsync(
        PromoteUserRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new PromoteUserCommand(r.AccountancyId, r.UserId))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
