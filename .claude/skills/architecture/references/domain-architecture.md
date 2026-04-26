# Camada: Domain

> É aqui que tudo começa. Nenhuma funcionalidade existe antes de ser modelada no Domain.
> Nunca pule esta camada. Nunca implemente lógica de negócio fora dela.

---

## Primitivos Base

Todo Aggregate e toda Entity herdam de tipos definidos em `Core.Domain.Primitives`:

```csharp
// src/Core/Core.Domain/Primitives/Entity.cs
namespace Core.Domain.Primitives;

// Entity — base for all objects with identity
// AggregateRoot is defined in the same file as Entity
Entity        → Guid Id, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt, bool IsDeleted

// AggregateRoot — inherits Entity, adds event sourcing
AggregateRoot → ulong Version, IDomainEvent queue, RaiseEvent<T>(), ApplyEvent()
```

Sempre referencie `Core.Domain.Primitives` — nunca redefina nem duplique estes tipos base.

---

## Anatomia do Aggregate

```csharp
public sealed class UserInvite : AggregateRoot
{
    // 1. Properties — public getters only (private set or init)
    public string Email { get; private set; } = default!;
    public UserRole Role { get; private set; } = default!;
    public InviteStatus Status { get; private set; } = default!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }

    // 2. Factory method — single creation point
    public static UserInvite Create(string email, UserRole role, Guid? accountancyId, string tokenHash)
    {
        var invite = new UserInvite();
        invite.RaiseEvent<DomainEvents.UserInviteCreated>(version => new(
            Id: Guid.NewGuid(),
            Email: email,
            Role: role.Name,                   // SmartEnum → string in the event
            AccountancyId: accountancyId,
            TokenHash: tokenHash,
            Status: InviteStatus.Pending.Name, // SmartEnum → string in the event
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(72),
            CreatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));
        return invite;
    }

    // 3. Behavior methods — mutate state via RaiseEvent, never directly
    public void Revoke()
        => ChangeStatus(InviteStatus.Revoked);

    public void Accept()
        => ChangeStatus(InviteStatus.Accepted);

    // 4. State reconstitution — one When() per event, no exceptions
    protected override void ApplyEvent(IDomainEvent @event)
        => When(@event as dynamic);

    private void When(DomainEvents.UserInviteCreated @event)
    {
        Id = @event.Id;
        Email = @event.Email;
        Role = UserRole.FromName(@event.Role);         // string → SmartEnum here
        Status = InviteStatus.FromName(@event.Status); // string → SmartEnum here
        ExpiresAt = @event.ExpiresAt;
        CreatedAt = @event.CreatedAt;
    }

    private void When(DomainEvents.UserInviteRevoked @event)
    {
        Status = InviteStatus.FromName(@event.Status);
        UsedAt = @event.UsedAt;
        UpdatedAt = @event.UpdatedAt;
    }
}
```

**Regras absolutas de aggregate:**
- Sem exceptions
- Mudanças de estado SOMENTE via `RaiseEvent<T>()` — nunca atribuição direta fora de `When()`
- Propriedades com `private set` ou `init` — nunca `public set`
- Um `When()` por evento — nenhum evento sem handler
- `When()` recebe `@event as dynamic` via `ApplyEvent()` — não substitua por switch/if

---

## Domain Events

Todos os eventos vivem em um único arquivo: `{Module}.Domain/Events/DomainEvents.cs`

```csharp
namespace Identity.Domain.Events;

public static class DomainEvents
{
    public sealed record UserInviteCreated(
        Guid Id,
        string Email,
        string Role,              // SmartEnum becomes string — ALWAYS
        Guid? AccountancyId,
        string TokenHash,
        string Status,            // SmartEnum becomes string — ALWAYS
        DateTimeOffset ExpiresAt,
        DateTimeOffset CreatedAt,
        ulong Version             // required — event sourcing
    ) : Message, IDomainEvent;

    public sealed record UserInviteRevoked(
        Guid Id,
        string Status,
        DateTimeOffset? UsedAt,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;
}
```

**Regras de Domain Event:**
- `sealed record` — imutável, sem herança
- Herda de `Message, IDomainEvent` — nesta ordem
- Tipos permitidos: `Guid`, `string`, `int`, `bool`, `decimal`, `DateTimeOffset`, `DateTimeOffset?`, coleções de primitivos
- Tipos proibidos: SmartEnum, entities, value objects, qualquer tipo de Domain
- SmartEnum → `string` (via `.Name`) no evento; a conversão `FromName()` acontece no `When()` do aggregate
- `Version` obrigatória em todo evento
- Todos os eventos do módulo em um único arquivo `DomainEvents.cs`

---

## Como RaiseEvent Funciona

```csharp
// Inside the aggregate — mandatory pattern:
RaiseEvent<DomainEvents.UserInviteRevoked>(version => new(
    Id: Id,
    Status: InviteStatus.Revoked.Name,   // .Name converts SmartEnum → string
    UsedAt: DateTimeOffset.UtcNow,
    UpdatedAt: DateTimeOffset.UtcNow,
    Version: version                      // version comes from the delegate — never hardcode it
));
```

`RaiseEvent<T>` faz duas coisas:
1. Chama `ApplyEvent()` → `When(event)` → muta o estado do aggregate
2. Enfileira o evento para ser persistido por `AppendEventsAsync`

**Nunca** chame `When()` diretamente. **Nunca** passe `Version` como valor fixo.

---

## Enumerações (SmartEnum)

Todos os módulos usam `Ardalis.SmartEnum`. Pattern obrigatório:

```csharp
// File: {Module}.Domain/Enumerations/{EnumName}.cs
namespace Identity.Domain.Enumerations;

public abstract class InviteStatus : SmartEnum<InviteStatus>
{
    public static readonly InviteStatus Pending  = new PendingStatus();
    public static readonly InviteStatus Accepted = new AcceptedStatus();
    public static readonly InviteStatus Expired  = new ExpiredStatus();
    public static readonly InviteStatus Revoked  = new RevokedStatus();

    private InviteStatus(string name, int value) : base(name, value) { }

    // Implicit conversions — required in all project SmartEnums
    public static implicit operator string(InviteStatus status) => status.Name;
    public static implicit operator int(InviteStatus status)    => status.Value;

    // Private sealed subclasses — one per value
    private sealed class PendingStatus()  : InviteStatus(nameof(Pending),  0);
    private sealed class AcceptedStatus() : InviteStatus(nameof(Accepted), 1);
    private sealed class ExpiredStatus()  : InviteStatus(nameof(Expired),  2);
    private sealed class RevokedStatus()  : InviteStatus(nameof(Revoked),  3);
}
```

**Regras de enumeração:**
- Classe `abstract` herdando de `SmartEnum<T>`
- Conversões implícitas para `string` e `int` são obrigatórias
- Subclasses `private sealed` para cada valor
- Valores `int` explícitos e estáveis — nunca reordene valores existentes
- Conversão no aggregate: `SmartEnum.FromName(@event.FieldName)` dentro de `When()`

---

## Projection Models

Modelos de leitura para MongoDB. Definidos dentro da camada Domain em `{Module}.Domain/Projections/ProjectionModel.cs`.

```csharp
namespace Identity.Domain.Projections;

public static class ProjectionModel
{
    public record User(
        Guid Id,
        string FirstName,
        string LastName,
        string Email,
        string Role,              // string — not UserRole SmartEnum
        Guid? AccountancyId,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt,
        bool IsDeleted = false
    ) : IProjectionModel;

    public record UserInvite(
        Guid Id,
        string Email,
        string Role,              // string — not UserRole SmartEnum
        Guid? AccountancyId,
        string TokenHash,
        string Status,            // string — not InviteStatus SmartEnum
        DateTimeOffset ExpiresAt,
        DateTimeOffset? UsedAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt,
        bool IsDeleted = false
    ) : IProjectionModel;
}
```

**Regras de Projection Model:**
- `static class ProjectionModel` envolvendo todos os records do módulo — um arquivo por módulo
- Cada modelo de leitura é um `record` que implementa `IProjectionModel`
- Todos os campos são primitivos — nunca SmartEnum, entities ou value objects
- `Role` e `Status` armazenados como `string` — mesma regra dos Domain Events
- `IsDeleted` sempre presente com padrão `false`
- Atualizado exclusivamente por Projection Handlers na camada Application — nunca escrito diretamente

---

## O que NUNCA fazer no Domain

**Mutação de estado**
- Nunca atribua estado diretamente (`invite.Status = ...`). Sempre mute estado por meio de um método de comportamento que chama `RaiseEvent<T>()`.
- Nunca chame `When()` diretamente. Ele é invocado apenas internamente por `ApplyEvent()`.
- Nunca hardcode `Version` em um evento (`Version: 1`). Sempre use o parâmetro do delegate fornecido por `RaiseEvent<T>()`.

**Domain Events**
- Nunca use tipos SmartEnum como campos de Domain Event. Sempre converta para `string` via `.Name` antes de levantar o evento.
- Nunca espalhe eventos em múltiplos arquivos. Todos os eventos de um módulo vivem em um único `DomainEvents.cs`.

**Estrutura do aggregate**
- Nunca declare construtor público em um Aggregate. Sempre use construtor `private` e método factory `Create()`.
- Nunca use `switch` ou `if` dentro de `ApplyEvent()`. Sempre faça dispatch via `When(@event as dynamic)` com um método `When()` por tipo de evento.

**Fronteiras de camada**
- Nunca coloque lógica de negócio na camada Application. Regras de negócio pertencem aos métodos do Aggregate.
- Nunca escreva em um Projection Model diretamente a partir de um método de Domain. Projections são atualizadas exclusivamente por Projection Handlers na camada Application.