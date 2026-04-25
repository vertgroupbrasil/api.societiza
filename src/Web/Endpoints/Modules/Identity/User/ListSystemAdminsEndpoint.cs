using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Application.Policies;
using Identity.Shared.Queries.User;
using Identity.Shared.Response.User;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Identity.User.Requests.Queries;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Identity.User;

[Authorize(Policy = IdentityPolicies.CanManageAccountancies)]
public sealed class ListSystemAdminsEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<ListSystemAdminsRequest>
    .WithActionResult<IReadOnlyList<UserDetailResponse>>
{
    [ApiVersion("1.0")]
    [HttpGet(IdentityRoutes.ListSystemAdmins)]
    [ProducesResponseType(typeof(IReadOnlyList<UserDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        OperationId = "ListSystemAdmins",
        Summary     = "Listar administradores do sistema",
        Description = """
            Retorna a lista de todos os SystemAdmins ativos, ordenados por data de criação decrescente.

            **Regras:**
            - Retorna apenas usuários ativos (não deletados).

            **Efeitos:** Nenhum.

            **Autorização:** SystemAdmin.
            """,
        Tags = [Tags.Identity.Users])]
    public override async Task<ActionResult<IReadOnlyList<UserDetailResponse>>> HandleAsync(
        ListSystemAdminsRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(_ => new ListSystemAdminsQuery())
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
