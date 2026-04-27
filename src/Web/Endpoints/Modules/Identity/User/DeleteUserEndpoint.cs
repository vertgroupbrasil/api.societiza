using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Application.Policies;
using Identity.Shared.Commands.User;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Identity.User.Requests.Commands;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Identity.User;

[Authorize(Policy = IdentityPolicies.CanManageUsers)]
public sealed class DeleteUserEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<DeleteUserRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpDelete(IdentityRoutes.DeleteUser)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [SwaggerOperation(
        OperationId = "DeleteUser",
        Summary     = "Excluir usuário",
        Description = """
            Realiza o soft-delete de um usuário e invalida imediatamente sua sessão.

            **Regras:**
            - AccountancyAdmin só pode excluir membros da própria contabilidade.
            - Não é possível excluir a si mesmo.
            - Não é possível excluir o último AccountancyAdmin de uma contabilidade.
            - Não é possível excluir o último SystemAdmin do sistema.
            - SystemAdmin pode informar `accountancyId` via query param para identificar o tenant.

            **Respostas:**
            - `200` - usuário excluído com sucesso.
            - `400` - dados inválidos ou ausentes.
            - `401` - access token ausente ou inválido.
            - `403` - usuário sem permissão.
            - `404` - usuário não encontrado.
            - `409` - operação viola regra de negócio (ex.: último admin, auto-exclusão).

            **Efeitos:** Soft-delete do usuário; tokens de refresh revogados e security stamp regenerado, invalidando a sessão do usuário removido.

            **Autorização:** SystemAdmin ou AccountancyAdmin.
            """,
        Tags = [Tags.Identity.Users])]
    public override async Task<ActionResult> HandleAsync(
        DeleteUserRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new DeleteUserCommand(r.UserId, r.AccountancyId))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(() => Ok(), this.HandleFailure);
}
