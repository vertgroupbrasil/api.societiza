# Tarefa 5: Endpoints de Autenticação

> Parte 5 da estruturação da Tarefa 4 da Fase 1 do Roadmap Societiza.
> Cobre os cinco endpoints de autenticação em cascata completo: rota → request DTO → endpoint → command → validator → handler.
> Gerado em 01/04/2026.

---

## Visão Geral

Este documento estrutura os cinco endpoints de autenticação do módulo Identity. Cada seção cobre uma rota completa em cascata do endpoint até o handler de Application.

**Dependências:**
- Documento 3 — entidades `RefreshToken` e `PasswordResetToken`, eventos de domínio e métodos do aggregate `User`.
- Documento 4 — projection models `ProjectionModel.RefreshToken` e `ProjectionModel.PasswordResetToken` disponíveis no MongoDB.

---

## Estrutura de Arquivos

```
Identity.Shared/
├── Commands/
│   └── Auth/
│       ├── LoginCommand.cs
│       ├── RefreshCommand.cs
│       ├── LogoutCommand.cs
│       ├── ForgotPasswordCommand.cs
│       └── ResetPasswordCommand.cs
├── Emails/
│   └── SendPasswordResetEmailCommand.cs
└── Response/
    └── Auth/
        ├── LoginResponse.cs
        └── RefreshResponse.cs

Identity.Application/
└── UseCases/
    ├── Commands/
    │   └── Auth/
    │       ├── LoginHandler.cs
    │       ├── RefreshHandler.cs
    │       ├── LogoutHandler.cs
    │       ├── ForgotPasswordHandler.cs
    │       └── ResetPasswordHandler.cs
    └── Validators/
        └── Auth/
            ├── LoginValidator.cs
            ├── RefreshValidator.cs
            ├── LogoutValidator.cs
            ├── ForgotPasswordValidator.cs
            └── ResetPasswordValidator.cs

Web/
├── Endpoints/
│   └── Modules/
│       └── Identity/
│           └── Auth/
│               ├── Requests/
│               │   ├── LoginRequest.cs
│               │   ├── ForgotPasswordRequest.cs
│               │   └── ResetPasswordRequest.cs
│               ├── LoginEndpoint.cs
│               ├── RefreshEndpoint.cs
│               ├── LogoutEndpoint.cs
│               ├── ForgotPasswordEndpoint.cs
│               └── ResetPasswordEndpoint.cs
└── Extensions/
    └── HttpContextExtensions.cs
```

---

## Rotas

**Localização:** `src/Web/Endpoints/Routes/IdentityRoutes.cs`

Adicionar ao arquivo existente:

```csharp
internal const string Login          = $"{BaseUri}/auth/login";
internal const string Refresh        = $"{BaseUri}/auth/refresh";
internal const string Logout         = $"{BaseUri}/auth/logout";
internal const string ForgotPassword = $"{BaseUri}/auth/forgot-password";
internal const string ResetPassword  = $"{BaseUri}/auth/reset-password";
```

---

## Cookie Helper

**Localização:** `src/Web/Extensions/HttpContextExtensions.cs`

Método de extensão que encapsula a lógica de cookie — evita repetição e mantém os endpoints limpos.

```csharp
namespace Web.Extensions;

internal static class HttpContextExtensions
{
    private const string RefreshTokenCookie = "refresh_token";

    internal static void SetRefreshTokenCookie(this HttpContext context, string token) =>
        context.Response.Cookies.Append(RefreshTokenCookie, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });

    internal static void DeleteRefreshTokenCookie(this HttpContext context) =>
        context.Response.Cookies.Delete(RefreshTokenCookie);
}
```

---

## 1. POST /auth/login

**Rota:** `POST /api/v1/identity/auth/login`
**Autenticação:** Pública

### Fluxo

```
LoginRequest (body: email, password)
  → LoginCommand(Email, Password)
    → LoginValidator — valida campos obrigatórios
      → LoginHandler
          → userProjection.FindAsync(x => x.Email == email) → obtém Id
          → LoadAggregateAsync<User>(id) → carrega aggregate
          → passwordService.VerifyPassword(password, user.PasswordHash)
          → tokenService.GenerateAccessToken(user) → accessToken
          → tokenService.GenerateRefreshToken(user) → refreshTokenJwt
          → SHA-256(refreshTokenJwt) → tokenHash
          → user.AddRefreshToken(tokenHash, expiresAt)
          → AppendEventsAsync(user)
          → retorna LoginResponse(AccessToken, RefreshToken)
            → endpoint seta cookie HttpOnly "refresh_token" = RefreshToken
            → endpoint retorna Ok com AccessToken no body
```

### Response

**Body:** `{ accessToken: "..." }`
**Cookie:** `refresh_token` — `HttpOnly`, `Secure`, `SameSite=Strict`, expiração = 7 dias

### Request DTO

**Localização:** `src/Web/Endpoints/Modules/Identity/Auth/Requests/LoginRequest.cs`

```csharp
namespace Web.Endpoints.Modules.Identity.Auth.Requests;

public sealed class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

### Endpoint

**Localização:** `src/Web/Endpoints/Modules/Identity/Auth/LoginEndpoint.cs`

```csharp
using Ardalis.ApiEndpoints;
using Identity.Shared.Commands.Auth;
using Identity.Shared.Response.Auth;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Identity.Auth.Requests;
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
        Summary = "Login",
        Description = "Authenticates the user and returns an access token. Sets the refresh token as an HttpOnly cookie.",
        Tags = [Tags.Identity])]
    public override async Task<ActionResult<LoginResponse>> HandleAsync(
        LoginRequest request, CancellationToken cancellationToken)
        => await Result.Create(request)
            .Map(r => new LoginCommand(Email: r.Email, Password: r.Password))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(response =>
            {
                httpContextAccessor.HttpContext!.SetRefreshTokenCookie(response.RefreshToken);
                return Ok(new LoginResponse(response.AccessToken));
            },
            this.HandleFailure);
}
```

### Command

**Localização:** `src/Modules/Identity/Identity.Shared/Commands/Auth/LoginCommand.cs`

```csharp
using Core.Application.Messaging.Commands;
using Identity.Shared.Response.Auth;

namespace Identity.Shared.Commands.Auth;

public sealed record LoginCommand(
    string Email,
    string Password
) : ICommand<LoginResponse>;
```

### Response DTO

**Localização:** `src/Modules/Identity/Identity.Shared/Response/Auth/LoginResponse.cs`

O handler retorna `LoginResponse` com os dois tokens. O endpoint usa `RefreshToken` para o cookie e expõe apenas `AccessToken` no body.

```csharp
namespace Identity.Shared.Response.Auth;

public sealed record LoginResponse(string AccessToken, string RefreshToken);
```

### Validator

**Localização:** `src/Modules/Identity/Identity.Application/UseCases/Validators/Auth/LoginValidator.cs`

```csharp
using FluentValidation;
using Identity.Shared.Commands.Auth;

namespace Identity.Application.UseCases.Validators.Auth;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be valid");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}
```

### Handler

**Localização:** `src/Modules/Identity/Identity.Application/UseCases/Commands/Auth/LoginHandler.cs`

```csharp
using System.Security.Cryptography;
using System.Text;
using Core.Application.Messaging.Commands;
using Core.Shared.Errors;
using Core.Shared.Results;
using Identity.Application.Services;
using Identity.Application.Services.Abstractions;
using Identity.Domain.Aggregates;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Identity.Shared.Commands.Auth;
using Identity.Shared.Response.Auth;

namespace Identity.Application.UseCases.Commands.Auth;

public sealed class LoginHandler(
    IIdentityApplicationService applicationService,
    IIdentityProjection<ProjectionModel.User> userProjection,
    IPasswordService passwordService,
    ITokenService tokenService
) : ICommandHandler<LoginCommand, LoginResponse>
{
    public async Task<Result<LoginResponse>> Handle(
        LoginCommand request, CancellationToken cancellationToken)
    {
        var userProjectionModel = await userProjection.FindAsync(
            x => x.Email == request.Email,
            cancellationToken);

        if (userProjectionModel is null)
            return Result.Failure<LoginResponse>(new Error(
                "Auth.InvalidCredentials", "Invalid email or password"));

        var userResult = await applicationService.LoadAggregateAsync<User>(
            userProjectionModel.Id, cancellationToken);

        if (!userResult.IsSuccess)
            return Result.Failure<LoginResponse>(new Error(
                "Auth.InvalidCredentials", "Invalid email or password"));

        var user = userResult.Value;

        if (!passwordService.VerifyPassword(request.Password, user.PasswordHash!))
            return Result.Failure<LoginResponse>(new Error(
                "Auth.InvalidCredentials", "Invalid email or password"));

        var accessToken = tokenService.GenerateAccessToken(user);
        var refreshTokenJwt = tokenService.GenerateRefreshToken(user);
        var tokenHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(refreshTokenJwt)));

        user.AddRefreshToken(tokenHash, DateTimeOffset.UtcNow.AddDays(7));

        await applicationService.AppendEventsAsync(user, cancellationToken);

        return Result.Success(new LoginResponse(accessToken, refreshTokenJwt));
    }
}
```

> Erros de credenciais inválidas retornam sempre a mesma mensagem genérica — nunca revelar se o email existe ou não.

---

## 2. POST /auth/refresh

**Rota:** `POST /api/v1/identity/auth/refresh`
**Autenticação:** Pública (cookie `refresh_token`)

### Fluxo

```
cookie: refresh_token
  → RefreshCommand(RefreshToken)
    → RefreshValidator — valida campo obrigatório
      → RefreshHandler
          → tokenService.ValidateRefreshToken(refreshTokenJwt) → ClaimsPrincipal
          → extrai sub (UserId) das claims
          → LoadAggregateAsync<User>(userId)
          → SHA-256(refreshTokenJwt) → tokenHash
          → busca RefreshToken no aggregate pelo tokenHash → valida IsValid()
          → user.RevokeRefreshToken(tokenId)
          → gera novo refreshTokenJwt
          → SHA-256(novoRefreshTokenJwt) → novoTokenHash
          → user.AddRefreshToken(novoTokenHash, expiresAt)
          → tokenService.GenerateAccessToken(user) → accessToken
          → AppendEventsAsync(user)
          → retorna RefreshResponse(AccessToken, RefreshToken)
            → endpoint seta novo cookie HttpOnly "refresh_token"
            → endpoint retorna Ok com AccessToken no body
```

### Endpoint

**Localização:** `src/Web/Endpoints/Modules/Identity/Auth/RefreshEndpoint.cs`

O cookie é lido do `HttpContext` antes do pipeline railway — `[FromCookie]` não é suportado nativamente pelo Ardalis Endpoints.

```csharp
using Ardalis.ApiEndpoints;
using Identity.Shared.Commands.Auth;
using Identity.Shared.Response.Auth;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
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
        Summary = "Refresh token",
        Description = "Rotates the refresh token. Reads the current token from the HttpOnly cookie and sets a new one.",
        Tags = [Tags.Identity])]
    public override async Task<ActionResult<RefreshResponse>> HandleAsync(
        CancellationToken cancellationToken)
    {
        var refreshToken = httpContextAccessor.HttpContext!.Request.Cookies["refresh_token"] ?? string.Empty;

        return await Result.Create(new RefreshCommand(RefreshToken: refreshToken))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(response =>
            {
                httpContextAccessor.HttpContext!.SetRefreshTokenCookie(response.RefreshToken);
                return Ok(new RefreshResponse(response.AccessToken));
            },
            this.HandleFailure);
    }
}
```

### Command

**Localização:** `src/Modules/Identity/Identity.Shared/Commands/Auth/RefreshCommand.cs`

```csharp
using Core.Application.Messaging.Commands;
using Identity.Shared.Response.Auth;

namespace Identity.Shared.Commands.Auth;

public sealed record RefreshCommand(string RefreshToken) : ICommand<RefreshResponse>;
```

### Response DTO

**Localização:** `src/Modules/Identity/Identity.Shared/Response/Auth/RefreshResponse.cs`

```csharp
namespace Identity.Shared.Response.Auth;

public sealed record RefreshResponse(string AccessToken, string RefreshToken);
```

### Validator

**Localização:** `src/Modules/Identity/Identity.Application/UseCases/Validators/Auth/RefreshValidator.cs`

```csharp
using FluentValidation;
using Identity.Shared.Commands.Auth;

namespace Identity.Application.UseCases.Validators.Auth;

public sealed class RefreshValidator : AbstractValidator<RefreshCommand>
{
    public RefreshValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required");
    }
}
```

### Handler

**Localização:** `src/Modules/Identity/Identity.Application/UseCases/Commands/Auth/RefreshHandler.cs`

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Core.Application.Messaging.Commands;
using Core.Shared.Errors;
using Core.Shared.Results;
using Identity.Application.Services;
using Identity.Application.Services.Abstractions;
using Identity.Domain.Aggregates;
using Identity.Shared.Commands.Auth;
using Identity.Shared.Response.Auth;

namespace Identity.Application.UseCases.Commands.Auth;

public sealed class RefreshHandler(
    IIdentityApplicationService applicationService,
    ITokenService tokenService
) : ICommandHandler<RefreshCommand, RefreshResponse>
{
    public async Task<Result<RefreshResponse>> Handle(
        RefreshCommand request, CancellationToken cancellationToken)
    {
        var principal = tokenService.ValidateRefreshToken(request.RefreshToken);

        if (principal is null)
            return Result.Failure<RefreshResponse>(new Error(
                "Auth.InvalidToken", "Refresh token is invalid or expired"));

        var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (userIdClaim is null)
            return Result.Failure<RefreshResponse>(new Error(
                "Auth.InvalidToken", "Refresh token is invalid or expired"));

        var userId = Guid.Parse(userIdClaim);

        var userResult = await applicationService.LoadAggregateAsync<User>(userId, cancellationToken);

        if (!userResult.IsSuccess)
            return Result.Failure<RefreshResponse>(new Error(
                "Auth.InvalidToken", "Refresh token is invalid or expired"));

        var user = userResult.Value;

        var tokenHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(request.RefreshToken)));

        var existingToken = user.RefreshTokens.FirstOrDefault(t => t.TokenHash == tokenHash);

        if (existingToken is null || !existingToken.IsValid())
            return Result.Failure<RefreshResponse>(new Error(
                "Auth.InvalidToken", "Refresh token is invalid or expired"));

        user.RevokeRefreshToken(existingToken.Id);

        var newRefreshTokenJwt = tokenService.GenerateRefreshToken(user);
        var newTokenHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(newRefreshTokenJwt)));

        user.AddRefreshToken(newTokenHash, DateTimeOffset.UtcNow.AddDays(7));

        var accessToken = tokenService.GenerateAccessToken(user);

        await applicationService.AppendEventsAsync(user, cancellationToken);

        return Result.Success(new RefreshResponse(accessToken, newRefreshTokenJwt));
    }
}
```

---

## 3. POST /auth/logout

**Rota:** `POST /api/v1/identity/auth/logout`
**Autenticação:** `[Authorize]`

### Fluxo

```
cookie: refresh_token
  → LogoutCommand(RefreshToken)
    → LogoutValidator — valida campo obrigatório
      → LogoutHandler
          → tokenService.ValidateRefreshToken(refreshTokenJwt) → ClaimsPrincipal
          → extrai sub (UserId) das claims
          → LoadAggregateAsync<User>(userId)
          → SHA-256(refreshTokenJwt) → tokenHash
          → busca RefreshToken no aggregate pelo tokenHash
          → user.RevokeRefreshToken(tokenId)
          → AppendEventsAsync(user)
            → endpoint deleta cookie "refresh_token"
```

### Endpoint

**Localização:** `src/Web/Endpoints/Modules/Identity/Auth/LogoutEndpoint.cs`

```csharp
using Ardalis.ApiEndpoints;
using Identity.Shared.Commands.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
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
    [SwaggerOperation(
        Summary = "Logout",
        Description = "Revokes the current refresh token and clears the HttpOnly cookie.",
        Tags = [Tags.Identity])]
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
```

### Command

**Localização:** `src/Modules/Identity/Identity.Shared/Commands/Auth/LogoutCommand.cs`

```csharp
using Core.Application.Messaging.Commands;

namespace Identity.Shared.Commands.Auth;

public sealed record LogoutCommand(string RefreshToken) : ICommand;
```

### Validator

**Localização:** `src/Modules/Identity/Identity.Application/UseCases/Validators/Auth/LogoutValidator.cs`

```csharp
using FluentValidation;
using Identity.Shared.Commands.Auth;

namespace Identity.Application.UseCases.Validators.Auth;

public sealed class LogoutValidator : AbstractValidator<LogoutCommand>
{
    public LogoutValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required");
    }
}
```

### Handler

**Localização:** `src/Modules/Identity/Identity.Application/UseCases/Commands/Auth/LogoutHandler.cs`

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Core.Application.Messaging.Commands;
using Core.Shared.Errors;
using Core.Shared.Results;
using Identity.Application.Services;
using Identity.Application.Services.Abstractions;
using Identity.Domain.Aggregates;
using Identity.Shared.Commands.Auth;

namespace Identity.Application.UseCases.Commands.Auth;

public sealed class LogoutHandler(
    IIdentityApplicationService applicationService,
    ITokenService tokenService
) : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var principal = tokenService.ValidateRefreshToken(request.RefreshToken);

        if (principal is null)
            return Result.Failure(new Error(
                "Auth.InvalidToken", "Refresh token is invalid or expired"));

        var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (userIdClaim is null)
            return Result.Failure(new Error(
                "Auth.InvalidToken", "Refresh token is invalid or expired"));

        var userId = Guid.Parse(userIdClaim);

        var userResult = await applicationService.LoadAggregateAsync<User>(userId, cancellationToken);

        if (!userResult.IsSuccess)
            return Result.Failure(new Error(
                "Auth.InvalidToken", "Refresh token is invalid or expired"));

        var user = userResult.Value;

        var tokenHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(request.RefreshToken)));

        var existingToken = user.RefreshTokens.FirstOrDefault(t => t.TokenHash == tokenHash);

        if (existingToken is null || !existingToken.IsValid())
            return Result.Failure(new Error(
                "Auth.InvalidToken", "Refresh token is invalid or expired"));

        user.RevokeRefreshToken(existingToken.Id);

        await applicationService.AppendEventsAsync(user, cancellationToken);

        return Result.Success();
    }
}
```

---

## 4. POST /auth/forgot-password

**Rota:** `POST /api/v1/identity/auth/forgot-password`
**Autenticação:** Pública

### Fluxo

```
ForgotPasswordRequest (body: email)
  → ForgotPasswordCommand(Email)
    → ForgotPasswordValidator — valida email
      → ForgotPasswordHandler
          → userProjection.FindAsync(x => x.Email == email)
          → se não encontrado → retorna sucesso silencioso
          → LoadAggregateAsync<User>(id)
          → rawToken = RandomNumberGenerator
          → SHA-256(rawToken) → tokenHash
          → user.AddPasswordResetToken(tokenHash, expiresAt = UtcNow + 1h)
          → AppendEventsAsync(user)
          → jobScheduler.Enqueue(SendPasswordResetEmailCommand)
          → retorna sucesso
```

> Sempre retorna 200 independente de o email existir — nunca revelar a existência de contas.

### Request DTO

**Localização:** `src/Web/Endpoints/Modules/Identity/Auth/Requests/ForgotPasswordRequest.cs`

```csharp
namespace Web.Endpoints.Modules.Identity.Auth.Requests;

public sealed class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}
```

### Endpoint

**Localização:** `src/Web/Endpoints/Modules/Identity/Auth/ForgotPasswordEndpoint.cs`

```csharp
using Ardalis.ApiEndpoints;
using Identity.Shared.Commands.Auth;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Identity.Auth.Requests;

namespace Web.Endpoints.Modules.Identity.Auth;

public sealed class ForgotPasswordEndpoint(ISender sender)
    : EndpointBaseAsync
        .WithRequest<ForgotPasswordRequest>
        .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpPost(IdentityRoutes.ForgotPassword)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(
        Summary = "Forgot password",
        Description = "Sends a password reset email if the account exists. Always returns 200 to prevent email enumeration.",
        Tags = [Tags.Identity])]
    public override async Task<ActionResult> HandleAsync(
        ForgotPasswordRequest request, CancellationToken cancellationToken)
        => await Result.Create(request)
            .Map(r => new ForgotPasswordCommand(Email: r.Email))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(() => Ok(), this.HandleFailure);
}
```

### Command

**Localização:** `src/Modules/Identity/Identity.Shared/Commands/Auth/ForgotPasswordCommand.cs`

```csharp
using Core.Application.Messaging.Commands;

namespace Identity.Shared.Commands.Auth;

public sealed record ForgotPasswordCommand(string Email) : ICommand;
```

### Email Command

**Localização:** `src/Modules/Identity/Identity.Shared/Emails/SendPasswordResetEmailCommand.cs`

```csharp
using Core.Application.Messaging.Commands;

namespace Identity.Shared.Emails;

public sealed record SendPasswordResetEmailCommand(
    string ToEmail,
    string ToName,
    string ResetLink
) : ICommand;
```

### Validator

**Localização:** `src/Modules/Identity/Identity.Application/UseCases/Validators/Auth/ForgotPasswordValidator.cs`

```csharp
using FluentValidation;
using Identity.Shared.Commands.Auth;

namespace Identity.Application.UseCases.Validators.Auth;

public sealed class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be valid");
    }
}
```

### Handler

**Localização:** `src/Modules/Identity/Identity.Application/UseCases/Commands/Auth/ForgotPasswordHandler.cs`

```csharp
using System.Security.Cryptography;
using System.Text;
using Core.Application.Messaging.Commands;
using Core.Application.Services.Interfaces;
using Core.Infrastructure.Options;
using Core.Shared.Results;
using Identity.Application.Services;
using Identity.Domain.Aggregates;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Identity.Shared.Commands.Auth;
using Identity.Shared.Emails;
using Microsoft.Extensions.Options;

namespace Identity.Application.UseCases.Commands.Auth;

public sealed class ForgotPasswordHandler(
    IIdentityApplicationService applicationService,
    IIdentityProjection<ProjectionModel.User> userProjection,
    IJobSchedulerService jobScheduler,
    IOptions<FrontendOptions> frontendOptions
) : ICommandHandler<ForgotPasswordCommand>
{
    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var userProjectionModel = await userProjection.FindAsync(
            x => x.Email == request.Email,
            cancellationToken);

        if (userProjectionModel is null)
            return Result.Success();

        var userResult = await applicationService.LoadAggregateAsync<User>(
            userProjectionModel.Id, cancellationToken);

        if (!userResult.IsSuccess)
            return Result.Success();

        var user = userResult.Value;

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        user.AddPasswordResetToken(tokenHash, expiresAt);

        await applicationService.AppendEventsAsync(user, cancellationToken);

        var resetLink = $"{frontendOptions.Value.BaseUrl}/auth/reset-password?token={rawToken}";

        jobScheduler.Enqueue(new SendPasswordResetEmailCommand(
            ToEmail: request.Email,
            ToName: request.Email,
            ResetLink: resetLink
        ));

        return Result.Success();
    }
}
```

---

## 5. POST /auth/reset-password

**Rota:** `POST /api/v1/identity/auth/reset-password`
**Autenticação:** Pública (token no body)

### Fluxo

```
ResetPasswordRequest (body: token, newPassword)
  → ResetPasswordCommand(Token, NewPassword)
    → ResetPasswordValidator — valida campos obrigatórios
      → ResetPasswordHandler
          → SHA-256(token) → tokenHash
          → passwordResetTokenProjection.FindAsync(x => x.TokenHash == tokenHash) → obtém UserId
          → LoadAggregateAsync<User>(userId)
          → busca PasswordResetToken no aggregate pelo tokenHash → valida IsValid()
          → user.UsePasswordResetToken(tokenId)
          → passwordService.HashPassword(newPassword) → newPasswordHash
          → user.UpdatePassword(newPasswordHash)
          → AppendEventsAsync(user)
```

### Request DTO

**Localização:** `src/Web/Endpoints/Modules/Identity/Auth/Requests/ResetPasswordRequest.cs`

```csharp
namespace Web.Endpoints.Modules.Identity.Auth.Requests;

public sealed class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
```

### Endpoint

**Localização:** `src/Web/Endpoints/Modules/Identity/Auth/ResetPasswordEndpoint.cs`

```csharp
using Ardalis.ApiEndpoints;
using Identity.Shared.Commands.Auth;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Identity.Auth.Requests;

namespace Web.Endpoints.Modules.Identity.Auth;

public sealed class ResetPasswordEndpoint(ISender sender)
    : EndpointBaseAsync
        .WithRequest<ResetPasswordRequest>
        .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpPost(IdentityRoutes.ResetPassword)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Reset password",
        Description = "Consumes the password reset token and sets a new password.",
        Tags = [Tags.Identity])]
    public override async Task<ActionResult> HandleAsync(
        ResetPasswordRequest request, CancellationToken cancellationToken)
        => await Result.Create(request)
            .Map(r => new ResetPasswordCommand(Token: r.Token, NewPassword: r.NewPassword))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(() => Ok(), this.HandleFailure);
}
```

### Command

**Localização:** `src/Modules/Identity/Identity.Shared/Commands/Auth/ResetPasswordCommand.cs`

```csharp
using Core.Application.Messaging.Commands;

namespace Identity.Shared.Commands.Auth;

public sealed record ResetPasswordCommand(
    string Token,
    string NewPassword
) : ICommand;
```

### Validator

**Localização:** `src/Modules/Identity/Identity.Application/UseCases/Validators/Auth/ResetPasswordValidator.cs`

```csharp
using FluentValidation;
using Identity.Shared.Commands.Auth;

namespace Identity.Application.UseCases.Validators.Auth;

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters");
    }
}
```

### Handler

**Localização:** `src/Modules/Identity/Identity.Application/UseCases/Commands/Auth/ResetPasswordHandler.cs`

```csharp
using System.Security.Cryptography;
using System.Text;
using Core.Application.Messaging.Commands;
using Core.Shared.Errors;
using Core.Shared.Results;
using Identity.Application.Services;
using Identity.Application.Services.Abstractions;
using Identity.Domain.Aggregates;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Identity.Shared.Commands.Auth;

namespace Identity.Application.UseCases.Commands.Auth;

public sealed class ResetPasswordHandler(
    IIdentityApplicationService applicationService,
    IIdentityProjection<ProjectionModel.PasswordResetToken> passwordResetTokenProjection,
    IPasswordService passwordService
) : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));

        var tokenProjectionModel = await passwordResetTokenProjection.FindAsync(
            x => x.TokenHash == tokenHash,
            cancellationToken);

        if (tokenProjectionModel is null)
            return Result.Failure(new NotFoundError(new(
                "Auth.InvalidToken", "Password reset token is invalid or expired")));

        var userResult = await applicationService.LoadAggregateAsync<User>(
            tokenProjectionModel.UserId, cancellationToken);

        if (!userResult.IsSuccess)
            return Result.Failure(userResult.Error);

        var user = userResult.Value;

        var resetToken = user.PasswordResetTokens.FirstOrDefault(t => t.TokenHash == tokenHash);

        if (resetToken is null || !resetToken.IsValid())
            return Result.Failure(new NotFoundError(new(
                "Auth.InvalidToken", "Password reset token is invalid or expired")));

        user.UsePasswordResetToken(resetToken.Id);

        var newPasswordHash = passwordService.HashPassword(request.NewPassword);
        user.UpdatePassword(newPasswordHash);

        await applicationService.AppendEventsAsync(user, cancellationToken);

        return Result.Success();
    }
}
```

---

*Gerado em 01/04/2026.*