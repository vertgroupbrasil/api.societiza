using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Shared.Commands.Auth;
using Identity.Shared.Response.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Identity.Auth.Requests;
using Web.Endpoints.Routes;
using Web.Extensions;

namespace Web.Endpoints.Modules.Identity.Auth;

public sealed class LoginEndpoint(ISender sender, IHttpContextAccessor httpContextAccessor)
    : EndpointBaseAsync
        .WithRequest<LoginRequest>
        .WithActionResult<LoginResponse>
{
    [ApiVersion("1.0")]
    [HttpPost(IdentityRoutes.Login)]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [SwaggerOperation(
        OperationId = "Login",
        Summary     = "Fazer login",
        Description = """
            Autentica por e-mail e senha. Em caso de sucesso, retorna um `accessToken` JWT no corpo e define um cookie HttpOnly `refresh_token` (Secure, SameSite=Strict).

            **Respostas:**
            - `200` — autenticação bem-sucedida; use `accessToken` no header `Authorization: Bearer`.
            - `400` — corpo inválido (campos ausentes ou malformados).
            - `401` — credenciais incorretas ou usuário inexistente.

            **Cookie `refresh_token`:** gerenciado automaticamente pelo browser. Use `POST /refresh` para renovar o access token sem reautenticar.
            """,
        Tags = [Tags.Identity.Auth])]
    public override async Task<ActionResult<LoginResponse>> HandleAsync(
        LoginRequest request, CancellationToken cancellationToken)
        => await Result.Create(request)
            .Map(r => new LoginCommand(Email: r.Email, Password: r.Password))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(response =>
            {
                httpContextAccessor.HttpContext!.SetRefreshTokenCookie(response.RefreshToken);
                return Ok(new { accessToken = response.AccessToken });
            },
            this.HandleFailure);
}