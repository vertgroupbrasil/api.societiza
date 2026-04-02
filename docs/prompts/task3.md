# Tarefa 3: Domínio e Migrations

> Parte 3 da estruturação da Tarefa 4 da Fase 1 do Roadmap Societiza.
> Cobre as entidades de suporte à autenticação, os novos eventos de domínio, a atualização do aggregate User e a migration correspondente.
> Gerado em 01/04/2026.

---

## Visão Geral

Este documento estende o aggregate `User` com suporte a refresh tokens e tokens de reset de senha. As entidades `RefreshToken` e `PasswordResetToken` seguem o mesmo padrão das entidades filhas do módulo `WorkflowTemplate` — factory estática, construtor privado, sem eventos próprios. Os eventos e a reconstrução de estado ficam no aggregate `User`.

---

## Fluxo

```
User.AddRefreshToken(tokenHash, expiresAt)
  → RaiseEvent(UserRefreshTokenAdded)
    → When(UserRefreshTokenAdded) → RefreshTokens.Add(...)

User.RevokeRefreshToken(tokenId)
  → RaiseEvent(UserRefreshTokenRevoked)
    → When(UserRefreshTokenRevoked) → RefreshTokens revoga o token correspondente

User.AddPasswordResetToken(tokenHash, expiresAt)
  → RaiseEvent(UserPasswordResetTokenAdded)
    → When(UserPasswordResetTokenAdded) → PasswordResetTokens.Add(...)

User.UsePasswordResetToken(tokenId)
  → RaiseEvent(UserPasswordResetTokenUsed)
    → When(UserPasswordResetTokenUsed) → PasswordResetTokens marca o token como usado

User.UpdatePassword(newPasswordHash)        ← já existente
  → RaiseEvent(UserPasswordUpdated)
    → When(UserPasswordUpdated) → atualiza PasswordHash
```

---

## 1. Entidades

### RefreshToken

**Localização:** `Identity.Domain/Entities/RefreshToken.cs`

```csharp
using Core.Domain.Primitives;

namespace Identity.Domain.Entities;

public class RefreshToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public RefreshToken(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAt)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
    }

    public static RefreshToken Create(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAt)
        => new(id, userId, tokenHash, expiresAt);

    public void Revoke(DateTimeOffset revokedAt)
    {
        IsRevoked = true;
        RevokedAt = revokedAt;
    }

    public bool IsValid() => !IsRevoked && ExpiresAt > DateTimeOffset.UtcNow;
}
```

---

### PasswordResetToken

**Localização:** `Identity.Domain/Entities/PasswordResetToken.cs`

```csharp
using Core.Domain.Primitives;

namespace Identity.Domain.Entities;

public class PasswordResetToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }

    public PasswordResetToken(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAt)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
    }

    public static PasswordResetToken Create(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAt)
        => new(id, userId, tokenHash, expiresAt);

    public void Use(DateTimeOffset usedAt)
    {
        IsUsed = true;
        UsedAt = usedAt;
    }

    public bool IsValid() => !IsUsed && ExpiresAt > DateTimeOffset.UtcNow;
}
```

---

## 2. Eventos de Domínio

**Localização:** `Identity.Domain/Events/DomainEvents.cs`

Adicionar ao arquivo de eventos existente:

```csharp
public sealed record UserRefreshTokenAdded(
    Guid UserId,
    Guid TokenId,
    string TokenHash,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    ulong Version
) : Message, IDomainEvent;

public sealed record UserRefreshTokenRevoked(
    Guid UserId,
    Guid TokenId,
    DateTimeOffset RevokedAt,
    ulong Version
) : Message, IDomainEvent;

public sealed record UserPasswordResetTokenAdded(
    Guid UserId,
    Guid TokenId,
    string TokenHash,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    ulong Version
) : Message, IDomainEvent;

public sealed record UserPasswordResetTokenUsed(
    Guid UserId,
    Guid TokenId,
    DateTimeOffset UsedAt,
    ulong Version
) : Message, IDomainEvent;

public sealed record UserPasswordUpdated(
    Guid Id,
    string PasswordHash,
    DateTimeOffset UpdatedAt,
    ulong Version
) : Message, IDomainEvent;
```

---

## 3. Aggregate User

**Localização:** `Identity.Domain/Aggregates/User.cs`

### Novas propriedades

```csharp
public List<RefreshToken> RefreshTokens { get; private set; } = [];
public List<PasswordResetToken> PasswordResetTokens { get; private set; } = [];
```

### Novos métodos

```csharp
public Guid AddRefreshToken(string tokenHash, DateTimeOffset expiresAt)
{
    var tokenId = Guid.NewGuid();

    RaiseEvent<DomainEvents.UserRefreshTokenAdded>(version => new(
        UserId: Id,
        TokenId: tokenId,
        TokenHash: tokenHash,
        ExpiresAt: expiresAt,
        CreatedAt: DateTimeOffset.UtcNow,
        Version: version
    ));

    return tokenId;
}

public void RevokeRefreshToken(Guid tokenId)
{
    RaiseEvent<DomainEvents.UserRefreshTokenRevoked>(version => new(
        UserId: Id,
        TokenId: tokenId,
        RevokedAt: DateTimeOffset.UtcNow,
        Version: version
    ));
}

public Guid AddPasswordResetToken(string tokenHash, DateTimeOffset expiresAt)
{
    var tokenId = Guid.NewGuid();

    RaiseEvent<DomainEvents.UserPasswordResetTokenAdded>(version => new(
        UserId: Id,
        TokenId: tokenId,
        TokenHash: tokenHash,
        ExpiresAt: expiresAt,
        CreatedAt: DateTimeOffset.UtcNow,
        Version: version
    ));

    return tokenId;
}

public void UsePasswordResetToken(Guid tokenId)
{
    RaiseEvent<DomainEvents.UserPasswordResetTokenUsed>(version => new(
        UserId: Id,
        TokenId: tokenId,
        UsedAt: DateTimeOffset.UtcNow,
        Version: version
    ));
}

public void UpdatePassword(string newPasswordHash)
{
    RaiseEvent<DomainEvents.UserPasswordUpdated>(version => new(
        Id: Id,
        PasswordHash: newPasswordHash,
        UpdatedAt: DateTimeOffset.UtcNow,
        Version: version
    ));
}
```

### Novos handlers When

```csharp
private void When(DomainEvents.UserRefreshTokenAdded @event)
    => RefreshTokens.Add(RefreshToken.Create(@event.TokenId, @event.UserId, @event.TokenHash, @event.ExpiresAt));

private void When(DomainEvents.UserRefreshTokenRevoked @event)
{
    var token = RefreshTokens.FirstOrDefault(t => t.Id == @event.TokenId);
    token?.Revoke(@event.RevokedAt);
}

private void When(DomainEvents.UserPasswordResetTokenAdded @event)
    => PasswordResetTokens.Add(PasswordResetToken.Create(@event.TokenId, @event.UserId, @event.TokenHash, @event.ExpiresAt));

private void When(DomainEvents.UserPasswordResetTokenUsed @event)
{
    var token = PasswordResetTokens.FirstOrDefault(t => t.Id == @event.TokenId);
    token?.Use(@event.UsedAt);
}

private void When(DomainEvents.UserPasswordUpdated @event)
{
    PasswordHash = @event.PasswordHash;
    UpdatedAt = @event.UpdatedAt;
}
```

---

## 4. Migration

**Localização:** `Identity.Persistence/Migrations/`

A migration cobre as duas novas entidades de suporte à autenticação.

### Comando

```bash
dotnet ef migrations add AddAuthTokenEntities \
  --project src/Modules/Identity/Identity.Persistence \
  --startup-project src/Web \
  --context IdentityDbContext
```

> Lembrar de exportar a connection string antes de executar — ver `SKILL.md` de infraestrutura.

```bash
# PowerShell
$env:ConnectionStrings__Default = "Server=localhost;Port=5432;Username=societiza;Password=societiza;Database=societiza;"
```

### Aplicar ao banco

```bash
dotnet ef database update \
  --project src/Modules/Identity/Identity.Persistence \
  --startup-project src/Web \
  --context IdentityDbContext
```

---

*Gerado em 01/04/2026.*