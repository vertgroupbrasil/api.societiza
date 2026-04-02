# Tarefa 1: Services e Middlewares

> Parte 1 da estruturação da Tarefa 4 da Fase 1 do Roadmap Societiza.
> Cobre os services de tokens e contexto de usuário, e o middleware de exceções.
> Gerado em 01/04/2026. Atualizado em 02/04/2026.

---

## Visão Geral

Este documento estrutura os services que suportam o fluxo de autenticação e o middleware centralizado de exceções. Os services implementados aqui são a base para o isolamento por tenant e os endpoints de autenticação, estruturados no Documento 3.

---

## Fluxo

```
ITokenService
  → gera access token + refresh token (JWT bruto)
  → valida refresh token recebido do cookie → retorna ClaimsPrincipal

ICurrentUserService
  → lê claims do HttpContext (zero I/O)
  → ResolveAccountancyId(routeAccountancyId)
      → SystemAdmin    : retorna accountancyId da rota
      → Outros roles   : valida claim == rota → retorna id ou lança ForbiddenException

ExceptionHandlingMiddleware
  → captura ForbiddenException  → 403
  → captura UnauthorizedException → 401
  → captura NotFoundException   → 404
  → captura demais exceções     → 500 com log
```

---

## 1. ITokenService

**Localização:** `Identity.Application/Services/Abstractions/ITokenService.cs`

Interface responsável pela geração e validação de tokens JWT. O service não conhece repositórios nem conceitos de domínio além do `User` — é puramente responsável por gerar e validar JWTs.

O `GenerateRefreshToken` retorna apenas o JWT bruto. A responsabilidade de fazer o SHA-256, instanciar o `RefreshToken` e persistir é do handler de Application — não do `TokenService`.

```csharp
using System.Security.Claims;
using Identity.Domain.Aggregates;

namespace Identity.Application.Services.Abstractions;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken(User user);
    ClaimsPrincipal? ValidateRefreshToken(string token);
}
```

### Claims

**Access Token:**
| Claim | Valor |
|---|---|
| `sub` | `User.Id` |
| `email` | `User.Email` |
| `role` | `User.Role.Name` |
| `accountancy_id` | `User.AccountancyId` (omitido para `SystemAdmin`) |

**Refresh Token:**
| Claim | Valor |
|---|---|
| `sub` | `User.Id` |
| `jti` | `Guid` gerado no momento da emissão — permite busca direta do `RefreshToken` no banco pelo `Id` |

### Implementação

**Localização:** `Identity.Infrastructure/Services/TokenService.cs`

As expirações são constantes privadas — não ficam no `JwtOptions` nem nos appsettings.

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Identity.Application.Services.Abstractions;
using Identity.Domain.Aggregates;
using Identity.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Infrastructure.Services;

internal sealed class TokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    private const int AccessTokenExpirationMinutes = 15;
    private const int RefreshTokenExpirationDays = 7;

    public string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("role", user.Role.Name)
        };

        if (user.AccountancyId.HasValue)
            claims.Add(new("accountancy_id", user.AccountancyId.Value.ToString()));

        return GenerateToken(claims, TimeSpan.FromMinutes(AccessTokenExpirationMinutes));
    }

    public string GenerateRefreshToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        return GenerateToken(claims, TimeSpan.FromDays(RefreshTokenExpirationDays));
    }

    public ClaimsPrincipal? ValidateRefreshToken(string token)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_options.Key)),
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            return new JwtSecurityTokenHandler()
                .ValidateToken(token, parameters, out _);
        }
        catch
        {
            return null;
        }
    }

    private string GenerateToken(IEnumerable<Claim> claims, TimeSpan expiration)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(expiration),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### Registro

**Localização:** `Identity.Infrastructure/ServiceInstallers/InfrastructureServiceInstaller.cs`

Adicionar ao installer existente:

```csharp
services.TryAddTransient<ITokenService, TokenService>();
```

---

## 2. ICurrentUserService

**Localização:** `Core.Application/Services/Interfaces/ICurrentUserService.cs`

Interface que expõe o contexto do usuário autenticado lendo as claims do `HttpContext`. Zero I/O — leitura pura das claims já em memória no token. Disponível para todos os módulos via `Core`.

`Role` é exposto como `string` — o `Core` não tem conhecimento sobre `UserRole` nem sobre o módulo Identity. Quem precisar comparar com um role específico usa `IdentityRoles` (definido abaixo) ou faz a conversão para `UserRole` dentro do próprio módulo.

```csharp
namespace Core.Application.Services.Interfaces;

public interface ICurrentUserService
{
    Guid UserId { get; }
    string Email { get; }
    string Role { get; }
    Guid? AccountancyId { get; }
    Guid? ResolveAccountancyId(Guid? routeAccountancyId);
}
```

---

## 3. IdentityRoles

**Localização:** `Core.Domain/Constants/IdentityRoles.cs`

Constantes dos nomes de role usadas pelo `Core` para lógica estrutural — especificamente pelo `ResolveAccountancyId`. Evita magic strings sem criar dependência de `Identity.Domain`.

```csharp
namespace Core.Domain.Constants;

public static class IdentityRoles
{
    public const string SystemAdmin = "SystemAdmin";
}
```

> Apenas `SystemAdmin` é necessário aqui — é o único role que o `Core` precisa conhecer para resolver o isolamento por tenant. Os demais roles (`AccountingAdmin`, `AccountingEmployee`) são detalhes do módulo Identity.

---

## 4. CurrentUserService

**Localização:** `Core.Infrastructure/Services/CurrentUserService.cs`

Implementação que lê as claims diretamente do `HttpContext`. A claim `"role"` chega como string — nenhuma conversão para `UserRole` é feita aqui.

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Core.Application.Services.Interfaces;
using Core.Domain.Constants;
using Core.Domain.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Core.Infrastructure.Services;

internal sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private ClaimsPrincipal User => accessor.HttpContext?.User
        ?? throw new InvalidOperationException("No active HTTP context.");

    public Guid UserId =>
        Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    public string Email =>
        User.FindFirstValue(JwtRegisteredClaimNames.Email)!;

    public string Role =>
        User.FindFirstValue("role")!;

    public Guid? AccountancyId
    {
        get
        {
            var value = User.FindFirstValue("accountancy_id");
            return value is not null ? Guid.Parse(value) : null;
        }
    }

    public Guid? ResolveAccountancyId(Guid? routeAccountancyId)
    {
        if (Role == IdentityRoles.SystemAdmin)
            return routeAccountancyId;

        if (routeAccountancyId is null)
            return AccountancyId;

        if (AccountancyId != routeAccountancyId)
            throw new ForbiddenException();

        return AccountancyId;
    }
}
```

### Registro

**Localização:** `Core.Infrastructure/ServiceInstallers/InfrastructureServiceInstaller.cs`

Adicionar ao installer existente:

```csharp
services.TryAddTransient<ICurrentUserService, CurrentUserService>();
```

---

## 5. Middlewares

### Exceções de Domínio

**Localização:** `Core.Domain/Exceptions/`

Exceções base usadas pelo `ExceptionHandlingMiddleware`. Servem como sinal semântico — o middleware captura e converte em resposta HTTP padronizada.

```csharp
namespace Core.Domain.Exceptions;

public sealed class ForbiddenException()
    : Exception("You do not have permission to access this resource.");

public sealed class UnauthorizedException()
    : Exception("Authentication is required to access this resource.");

public sealed class NotFoundException(string message)
    : Exception(message);
```

### ExceptionHandlingMiddleware

**Localização:** `Web/Middlewares/ExceptionHandlingMiddleware.cs`

Substitui o middleware existente. Remove os catches de `ValidationException`, `UnauthorizedAccessException` e `KeyNotFoundException` — o `ValidationPipelineBehavior` já absorve erros de validação via `Result`, e nenhum módulo lança as exceções genéricas do .NET diretamente. Passa a capturar as exceções de domínio semânticas de `Core.Domain`.

A classe torna-se `internal sealed` — é registrada via `MiddlewareExtensions` e não precisa de visibilidade pública.

```csharp
using Core.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Web.Middlewares;

internal sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ForbiddenException)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Forbidden",
                message = "You do not have permission to access this resource."
            });
        }
        catch (UnauthorizedException)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized",
                message = "Authentication is required to access this resource."
            });
        }
        catch (NotFoundException ex)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "NotFound",
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "InternalServerError",
                message = "An unexpected error occurred."
            });
        }
    }
}
```

### Registro no Pipeline

O `MiddlewareExtensions.cs` não requer alteração — o `ExceptionHandlingMiddleware` já está registrado nele. A ordem no pipeline está correta: `UseMiddlewares()` é chamado antes de `UseAuthentication()` e `UseAuthorization()` no `Program.cs`, garantindo que qualquer exceção nos middlewares subsequentes seja capturada.

```csharp
// Program.cs — ordem do pipeline (sem alteração necessária)
app.UseMiddlewares(); // contém ExceptionHandlingMiddleware
app.UseAuthentication();
app.UseAuthorization();
```

---

*Gerado em 01/04/2026. Atualizado em 02/04/2026.*
