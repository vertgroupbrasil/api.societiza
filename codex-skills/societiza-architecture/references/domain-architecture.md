# Layer: Domain

> This is where everything starts. No feature exists before it is modeled in the Domain.
> Never skip this layer. Never implement business logic outside of it.

---

## Base Primitives

Every Aggregate and Entity inherits from types defined in `Core.Domain.Primitives`:

```csharp
// src/Core/Core.Domain/Primitives/Entity.cs
namespace Core.Domain.Primitives;

// Entity — base for all objects with identity
// AggregateRoot is defined in the same file as Entity
Entity        → Guid Id, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt, bool IsDeleted

// AggregateRoot — inherits Entity, adds event sourcing
AggregateRoot → ulong Version, IDomainEvent queue, RaiseEvent<T>(), ApplyEvent()
```

Always reference `Core.Domain.Primitives` — never redefine or duplicate these base types.

---

## Aggregate Anatomy

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

**Absolute aggregate rules:**
- No exceptions
- State changes ONLY via `RaiseEvent<T>()` — never direct assignment outside of `When()`
- Properties with `private set` or `init` — never `public set`
- One `When()` per event — no event without a handler
- `When()` receives `@event as dynamic` via `ApplyEvent()` — do not replace with switch/if

---

## Domain Events

All events live in a single file: `{Module}.Domain/Events/DomainEvents.cs`

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

**Domain Event rules:**
- `sealed record` — immutable, no inheritance
- Inherits `Message, IDomainEvent` — in this order
- Allowed types: `Guid`, `string`, `int`, `bool`, `decimal`, `DateTimeOffset`, `DateTimeOffset?`, collections of primitives
- Forbidden types: SmartEnum, entities, value objects, any Domain type
- SmartEnum → `string` (via `.Name`) in the event; `FromName()` conversion happens in the aggregate's `When()`
- `Version` required in every event
- All module events in a single `DomainEvents.cs` file

---

## How RaiseEvent Works

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

`RaiseEvent<T>` does two things:
1. Calls `ApplyEvent()` → `When(event)` → mutates aggregate state
2. Enqueues the event to be persisted by `AppendEventsAsync`

**Never** call `When()` directly. **Never** pass `Version` as a fixed value.

---

## Enumerations (SmartEnum)

All modules use `Ardalis.SmartEnum`. Mandatory pattern:

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

**Enumeration rules:**
- `abstract` class inheriting `SmartEnum<T>`
- Implicit `string` and `int` conversions are mandatory
- `private sealed` subclasses for each value
- Stable explicit `int` values — never reorder existing values
- Aggregate conversion: `SmartEnum.FromName(@event.FieldName)` inside `When()`

---

## Projection Models

Read models for MongoDB. Defined inside the Domain layer at `{Module}.Domain/Projections/ProjectionModel.cs`.

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

**Projection Model rules:**
- `static class ProjectionModel` wrapping all records of the module — one file per module
- Each read model is a `record` implementing `IProjectionModel`
- All fields are primitives — never SmartEnum, entities, or value objects
- `Role` and `Status` stored as `string` — same rule as Domain Events
- `IsDeleted` always present with default `false`
- Updated exclusively by Projection Handlers in the Application layer — never written directly

---

## What to NEVER do in the Domain

**State mutation**
- Never assign state directly (`invite.Status = ...`). Always mutate state through a behavior method that calls `RaiseEvent<T>()`.
- Never call `When()` directly. It is only invoked internally by `ApplyEvent()`.
- Never hardcode `Version` in an event (`Version: 1`). Always use the delegate parameter provided by `RaiseEvent<T>()`.

**Domain Events**
- Never use SmartEnum types as Domain Event fields. Always convert to `string` via `.Name` before raising the event.
- Never spread events across multiple files. All events for a module live in a single `DomainEvents.cs`.

**Aggregate structure**
- Never declare a public constructor on an Aggregate. Always use a `private` constructor and a `Create()` factory method.
- Never use `switch` or `if` inside `ApplyEvent()`. Always dispatch via `When(@event as dynamic)` with one `When()` method per event type.

**Layer boundaries**
- Never place business logic in the Application layer. Business rules belong inside Aggregate methods.
- Never write to a Projection Model directly from a Domain method. Projections are updated exclusively by Projection Handlers in the Application layer.