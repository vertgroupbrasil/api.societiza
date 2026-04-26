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
    [SwaggerOperation(
        OperationId = "Login",
        Summary     = "Fazer login",
        Description = """
            Autentica por e-mail e senha.

            **Regras:**
            - Credenciais incorretas ou usuário inexistente retornam 400 sem distinção (evita enumeração de contas).
            - O `accessToken` deve ser enviado no header `Authorization: Bearer` em requisições autenticadas.

            **Respostas:**
            - `200` - autenticação bem-sucedida; retorna `accessToken` no corpo.
            - `400` - corpo inválido ou credenciais incorretas.

            **Efeitos:** Define o cookie HttpOnly `refresh_token` (Secure, SameSite=Strict). Use `POST /refresh` para renovar o access token sem reautenticar.

            **Autorização:** Público.
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