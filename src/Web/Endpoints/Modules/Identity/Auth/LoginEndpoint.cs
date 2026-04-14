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
            Autentica o usuário por e-mail e senha e retorna um access token.

            **Regras:** Retorna 401 quando as credenciais são inválidas ou o usuário não existe.
            **Efeitos:** Emite um access token no corpo da resposta e um refresh token como cookie HttpOnly (`refresh_token`, Secure, SameSite=Strict).
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
