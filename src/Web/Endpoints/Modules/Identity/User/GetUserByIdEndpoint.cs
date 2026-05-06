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
public sealed class GetUserByIdEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<GetUserByIdRequest>
    .WithActionResult<UserProfileResponse>
{
    [ApiVersion("1.0")]
    [HttpGet(IdentityRoutes.GetUserById)]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "GetUserById",
        Summary     = "Obter usuário por id",
        Description = """
            Retorna os dados do perfil de um usuário ativo identificado pelo id.

            **Regras:**
            - SystemAdmin pode visualizar qualquer usuário ativo (SystemAdmin, AccountancyAdmin ou AccountancyEmployee).
            - AccountancyAdmin só pode visualizar AccountancyAdmin e AccountancyEmployee da própria contabilidade.
            - AccountancyEmployee só pode visualizar o próprio usuário.
            - Usuários deletados não são retornados.

            **Valores de enumeração na resposta:**
            - `role`: `SystemAdmin` | `AccountancyAdmin` | `AccountancyEmployee`

            **Respostas:**
            - `200` - usuário encontrado; retorna o perfil completo.
            - `400` - identificador inválido ou ausente.
            - `401` - access token ausente ou inválido.
            - `403` - usuário sem permissão para visualizar o alvo.
            - `404` - usuário não encontrado.

            **Efeitos:** Nenhum.

            **Autorização:** Qualquer usuário autenticado (escopo filtrado por perfil).
            """,
        Tags = [Tags.Identity.Users])]
    public override async Task<ActionResult<UserProfileResponse>> HandleAsync(
        GetUserByIdRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new GetUserByIdQuery(r.UserId))
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
