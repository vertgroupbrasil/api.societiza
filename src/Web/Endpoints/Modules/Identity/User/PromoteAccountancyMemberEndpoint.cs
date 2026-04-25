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
public sealed class PromoteAccountancyMemberEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<PromoteAccountancyMemberRequest>
    .WithActionResult<UserDetailResponse>
{
    [ApiVersion("1.0")]
    [HttpPost(IdentityRoutes.PromoteAccountancyMember)]
    [ProducesResponseType(typeof(UserDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [SwaggerOperation(
        OperationId = "PromoteAccountancyMember",
        Summary     = "Promover membro a administrador da contabilidade",
        Description = """
            Promove um AccountancyEmployee a AccountancyAdmin dentro da contabilidade indicada.

            **Regras:**
            - Apenas AccountancyAdmin pode promover membros — SystemAdmin não tem acesso a esta ação.
            - O membro promovido deve pertencer à mesma contabilidade do solicitante.
            - Somente AccountancyEmployee pode ser promovido — usuários já administradores retornam 409.

            **Efeitos:** O perfil do usuário é atualizado para AccountancyAdmin imediatamente.

            **Autorização:** AccountancyAdmin.
            """,
        Tags = [Tags.Identity.Users])]
    public override async Task<ActionResult<UserDetailResponse>> HandleAsync(
        PromoteAccountancyMemberRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new PromoteUserToAccountancyAdminCommand(r.AccountancyId, r.UserId))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
