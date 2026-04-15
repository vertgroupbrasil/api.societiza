using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Shared.Commands.Auth;
using Identity.Shared.Response.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Routes;
using Web.Extensions;

namespace Web.Endpoints.Modules.Identity.Auth;

public sealed class RefreshEndpoint(ISender sender, IHttpContextAccessor httpContextAccessor)
    : EndpointBaseAsync
        .WithoutRequest
        .WithActionResult<RefreshResponse>
{
    [ApiVersion("1.0")]
    [HttpPost(IdentityRoutes.Refresh)]
    [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [SwaggerOperation(
        OperationId = "Refresh",
        Summary     = "Renovar access token",
        Description = """
            Rotaciona o refresh token e emite um novo access token.

            **Regras:** O refresh token é lido do cookie HttpOnly `refresh_token`. Retorna 401 quando o token é inválido, expirado ou já foi revogado.
            **Efeitos:** Revoga o refresh token anterior, emite um novo refresh token (setado como cookie HttpOnly) e retorna um novo access token no corpo.
            **Autorização:** Público (prova de posse via cookie).
            """,
        Tags = [Tags.Identity.Auth])]
    public override async Task<ActionResult<RefreshResponse>> HandleAsync(
        CancellationToken cancellationToken)
    {
        var refreshToken = httpContextAccessor.HttpContext!.Request.Cookies["refresh_token"] ?? string.Empty;

        return await Result.Create(new RefreshCommand(RefreshToken: refreshToken))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(response =>
            {
                httpContextAccessor.HttpContext!.SetRefreshTokenCookie(response.RefreshToken);
                return Ok(new { accessToken = response.AccessToken });
            },
            this.HandleFailure);
    }
}
