# Tarefa 2: Autenticação e Permissões

> Parte 2 da estruturação da Tarefa 4 da Fase 1 do Roadmap Societiza.
> Cobre a configuração completa de autenticação JWT e o sistema de policies de autorização por role.
> Gerado em 01/04/2026. Atualizado em 02/04/2026.

---

## Visão Geral

Este documento estrutura a infraestrutura de autenticação e autorização do sistema. Ao final desta parte, o pipeline JWT estará configurado, as policies de autorização por role estarão registradas, e os endpoints protegidos poderão ser decorados com `[Authorize(Policy = "...")]`.

---

## Dependência de Execução

**Este documento deve ser executado após o Documento 2 (Services e Middlewares).**

O `Program.cs` registra `HasRoleHandler` e `IdentityPolicies`, que vivem em `Identity.Application`. Para que o projeto compile, `Identity.Application` precisa existir com `ITokenService` definido — o que é feito no Documento 2. Além disso, `IdentityPolicies` referencia `IdentityRoles` de `Core.Domain.Constants`, também criado no Documento 2.

**Ordem correta:**
1. Documento 2 — `IdentityRoles`, exceções de domínio, `ICurrentUserService`, `ITokenService`
2. Este documento — `HasRoleRequirement`, `HasRoleHandler`, `IdentityPolicies`, pipeline JWT no `Program.cs`

---

## Fluxo

```
appsettings.json
  → JwtOptions (via JwtOptionsConfiguration)
    → Program.cs configura AddAuthentication + AddJwtBearer + AddAuthorization
      → HasRoleHandler valida a claim "role" contra os roles exigidos pela policy
        → [Authorize(Policy = IdentityPolicies.X)] nos endpoints
```

---

## 1. JwtOptions

**Localização:** `Identity.Infrastructure/Options/JwtOptions.cs`

O record permanece sem alterações. As expirações dos tokens **não ficam no `JwtOptions` nem nos appsettings** — são constantes privadas do `TokenService` (Documento 2).

```csharp
using System.ComponentModel.DataAnnotations;

namespace Identity.Infrastructure.Options;

public sealed record JwtOptions
{
    [Required]
    public required string Key { get; init; }

    [Required]
    public required string Issuer { get; init; }

    [Required]
    public required string Audience { get; init; }
}
```

> `JwtOptionsConfiguration`, `appsettings.Development.json` e `appsettings.Production.json` não precisam de alteração.

---

## 2. Program.cs

**Localização:** `src/Web/Program.cs`

A configuração de autenticação e autorização é feita diretamente no `Program.cs`.

**`ValidateIssuer`** verifica se o token foi emitido pela URL da API do Societiza. Rejeita tokens gerados por outros sistemas ou ambientes.

**`ValidateAudience`** verifica se o token foi emitido para `societiza-portal`. Rejeita tokens destinados a outra audiência.

**`ClockSkew = TimeSpan.Zero`** remove a tolerância padrão de 5 minutos do ASP.NET — o token expira exatamente no tempo definido.

**`JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear()`** garante que as claims cheguem com os nomes originais definidos na geração (ex: `"role"`, `"sub"`), sem remapeamento automático pelo ASP.NET.

**Remover** o registro duplicado do `JwtOptions` que existe hoje:

```csharp
// REMOVER esta linha:
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("JwtOptions"));
```

O `using Identity.Infrastructure.Options;` no topo do arquivo também deve ser removido após essa limpeza.

**Adicionar** após o bloco `InstallModulesFromAssemblies`:

```csharp
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        var jwtOptions = builder.Services
            .BuildServiceProvider() //NOSONAR
            .GetRequiredService<IOptions<JwtOptions>>().Value;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(IdentityPolicies.Register);
builder.Services.AddTransient<IAuthorizationHandler, HasRoleHandler>();
```

**Usings necessários no Program.cs:**

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Identity.Application.Authorization;
using Identity.Application.Policies;
using Identity.Infrastructure.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
```

---

## 3. HasRoleRequirement + HasRoleHandler

**Localização:** `Identity.Application/Requirements/HasRoleRequirement.cs`

Carrega os roles permitidos para uma policy. Usado pelo `HasRoleHandler` para validar a claim `role` do token.

```csharp
using Microsoft.AspNetCore.Authorization;

namespace Identity.Application.Requirements;

internal sealed class HasRoleRequirement(params string[] roles) : IAuthorizationRequirement
{
    public IReadOnlyList<string> Roles { get; } = roles;
}
```

**Localização:** `Identity.Application/Authorization/HasRoleHandler.cs`

Handler centralizado que valida se a claim `role` do token está entre os roles exigidos pela policy. Substitui o uso direto de `RequireRole` nas policies — mais explícito e testável.

```csharp
using Identity.Application.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace Identity.Application.Authorization;

internal sealed class HasRoleHandler : AuthorizationHandler<HasRoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HasRoleRequirement requirement)
    {
        var roleClaim = context.User.FindFirst("role")?.Value;

        if (roleClaim is not null && requirement.Roles.Contains(roleClaim))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
```

---

## 4. IdentityPolicies

**Localização:** `Identity.Application/Policies/IdentityPolicies.cs`

Classe estática com dois papéis: centraliza os nomes das policies como constantes (evita magic strings nos endpoints) e registra todas as policies usando `HasRoleRequirement`.

`IdentityPolicies` vive em `Identity.Application` e pode referenciar `UserRole` diretamente para nomear os roles nas policies — sem dependência proibida, pois está dentro do próprio módulo Identity.

```csharp
using Identity.Application.Requirements;
using Identity.Domain.Enumerations;
using Microsoft.AspNetCore.Authorization;

namespace Identity.Application.Policies;

public static class IdentityPolicies
{
    public const string CanInviteSystemAdmin        = nameof(CanInviteSystemAdmin);
    public const string CanInviteAccountingAdmin    = nameof(CanInviteAccountingAdmin);
    public const string CanInviteAccountingEmployee = nameof(CanInviteAccountingEmployee);
    public const string CanManageInvites            = nameof(CanManageInvites);
    public const string CanManageUsers              = nameof(CanManageUsers);
    public const string CanPromoteUser              = nameof(CanPromoteUser);
    public const string CanManageAccountancies      = nameof(CanManageAccountancies);
    public const string CanManageWorkflowTemplates  = nameof(CanManageWorkflowTemplates);

    public static void Register(AuthorizationOptions options)
    {
        options.AddPolicy(CanInviteSystemAdmin, policy =>
            policy.AddRequirements(new HasRoleRequirement(
                UserRole.SystemAdmin.Name)));

        options.AddPolicy(CanInviteAccountingAdmin, policy =>
            policy.AddRequirements(new HasRoleRequirement(
                UserRole.SystemAdmin.Name,
                UserRole.AccountingAdmin.Name)));

        options.AddPolicy(CanInviteAccountingEmployee, policy =>
            policy.AddRequirements(new HasRoleRequirement(
                UserRole.AccountingAdmin.Name)));

        options.AddPolicy(CanManageInvites, policy =>
            policy.AddRequirements(new HasRoleRequirement(
                UserRole.SystemAdmin.Name,
                UserRole.AccountingAdmin.Name)));

        options.AddPolicy(CanManageUsers, policy =>
            policy.AddRequirements(new HasRoleRequirement(
                UserRole.SystemAdmin.Name,
                UserRole.AccountingAdmin.Name)));

        options.AddPolicy(CanPromoteUser, policy =>
            policy.AddRequirements(new HasRoleRequirement(
                UserRole.AccountingAdmin.Name)));

        options.AddPolicy(CanManageAccountancies, policy =>
            policy.AddRequirements(new HasRoleRequirement(
                UserRole.SystemAdmin.Name)));

        options.AddPolicy(CanManageWorkflowTemplates, policy =>
            policy.AddRequirements(new HasRoleRequirement(
                UserRole.SystemAdmin.Name)));
    }
}
```

---

## 5. InfrastructureServiceInstaller

**Localização:** `Identity.Infrastructure/ServiceInstallers/InfrastructureServiceInstaller.cs`

Com autenticação e autorização configuradas no `Program.cs`, o installer registra apenas o que é interno ao módulo Identity. O `ITokenService` já foi adicionado aqui no Documento 2.

```csharp
using Core.Application.EventBus;
using Core.Infrastructure.EventBus;
using Identity.Application.Services.Abstractions;
using Identity.Infrastructure.Options.Configurations;
using Identity.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Identity.Infrastructure.ServiceInstallers;

internal sealed class InfrastructureServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureOptions<JwtOptionsConfiguration>();

        services.TryAddTransient<IEventBus, EventBus>();
        services.TryAddTransient<ITokenService, TokenService>();
    }
}
```

> O registro do `ITokenService` está aqui apenas como referência consolidada — ele é criado no Documento 2 e este installer já o terá quando este documento for executado.

---

*Gerado em 01/04/2026. Atualizado em 02/04/2026.*
