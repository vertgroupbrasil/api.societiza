using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Shared.Commands.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Routes;
using Web.Extensions;

namespace Web.Endpoints.Modules.Identity.Auth;

[Authorize]
public sealed class LogoutEndpoint(ISender sender, IHttpContextAccessor httpContextAccessor)
    : EndpointBaseAsync
        .WithoutRequest
        .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpPost(IdentityRoutes.Logout)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        OperationId = "Logout",
        Summary     = "Efetuar logout",
        Description = """
            Revoga o refresh token atual e limpa o cookie `refresh_token` no cliente.

            **Respostas:**
            - `200` — sessão encerrada com sucesso.
            - `401` — access token ausente ou inválido.
            - `403` — token válido mas sem permissão.

            **Após o logout:** descarte o `accessToken` em memória no cliente — ele expira naturalmente pelo TTL do JWT.
            """,
        Tags = [Tags.Identity.Auth])]
    public override async Task<ActionResult> HandleAsync(CancellationToken cancellationToken)
    {
        var refreshToken = httpContextAccessor.HttpContext!.Request.Cookies["refresh_token"] ?? string.Empty;

        return await Result.Create(new LogoutCommand(RefreshToken: refreshToken))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(() =>
            {
                httpContextAccessor.HttpContext!.DeleteRefreshTokenCookie();
                return Ok();
            },
            this.HandleFailure);
    }
}