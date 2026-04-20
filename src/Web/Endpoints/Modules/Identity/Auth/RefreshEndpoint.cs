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
            Rotaciona o par access/refresh token usando o cookie `refresh_token`. Não requer `Authorization` header.

            **Respostas:**
            - `200` — novo `accessToken` no corpo; novo `refresh_token` setado como cookie HttpOnly.
            - `400` — cookie `refresh_token` ausente.
            - `401` — token inválido, expirado ou já revogado (single-use).

            **Estratégia recomendada:** chame este endpoint assim que receber `401` em qualquer outra requisição. Se Refresh também retornar `401`, redirecione para login.
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