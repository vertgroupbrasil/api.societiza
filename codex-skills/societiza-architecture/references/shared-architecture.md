# Layer: Shared

> The Shared layer exposes everything that other parts of the system need from this module — Commands, and Response DTOs — without creating circular dependencies.
> Depends only on Core. Never references the module's Domain or Application layers.

---

## Structure

```
{Module}.Shared/
├── Commands/        → ICommand and ICommand<TResponse> definitions
├── Queries/         → IQuery<TResponse> definitions (if the module has queries)
├── Emails/          → email delivery commands
└── Response/        → sealed record Response DTOs
```

---

## Commands

All Commands are `sealed record` implementing `ICommand` or `ICommand<TResponse>`.

### Command without response

Use for mutations that do not return data:

```csharp
// src/Modules/Identity/Identity.Shared/Commands/RevokeInviteCommand.cs
namespace Identity.Shared.Commands;

public sealed record RevokeInviteCommand(Guid InviteId) : ICommand;
```

### Command with response

Use when the command creates a resource and the caller needs its Id:

```csharp
// src/Modules/Identity/Identity.Shared/Commands/InviteUserCommand.cs
namespace Identity.Shared.Commands;

public sealed record InviteUserCommand(
    string Email,
    string Role,
    Guid? AccountancyId
) : ICommand<IdentifierResponse>;
```

`IdentifierResponse` is the standard return type for resource creation — defined in `Core.Shared`.

---

## Email Commands

Live in `{Module}.Shared/Emails/` — not in `Commands/` — because they are delivery commands, not business commands.

```csharp
// src/Modules/Identity/Identity.Shared/Emails/SendInviteEmailCommand.cs
namespace Identity.Shared.Emails;

public sealed record SendInviteEmailCommand(
    string ToEmail,
    string ToName,
    string InviteLink
) : ICommand;
```

**All fields must be primitive types (`string`, `int`, `Guid`, `bool`).**
Hangfire must serialize and deserialize the command — no domain objects, no SmartEnums, no complex types.

---

## Response DTOs

Response DTOs are returned by Query handlers and exposed through endpoints. They live in `{Module}.Shared/Response/`.

```csharp
// src/Modules/Accountancy/Accountancy.Shared/Response/AccountancyDetailResponse.cs
namespace Accountancy.Shared.Response;

public sealed record AccountancyDetailResponse(
    Guid Id,
    string CNPJ,
    string LegalName,
    string? TradeName,
    string Address,
    string City,
    string State,
    string PostalCode,
    string Phone,
    string? Email,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);
```

**Response rules:**
- `sealed record` — no inheritance, no interfaces
- All fields are primitives, `Guid`, `DateTimeOffset`, or nullable variants — never Domain types or SmartEnums
- Named after the resource and intent: `{Resource}DetailResponse`, `{Resource}ListResponse`, `{Resource}SummaryResponse`
- Defined in `{Module}.Shared/Response/` — never in Application or Web

---

## What to NEVER do in Shared

- Never reference the module's Domain or Application layers from Shared.
- Never use SmartEnum, entities, or value objects as Command or Response fields — primitives only.
- Never define Response DTOs in Application or Web — they always belong in Shared.
- Never place email commands in `Commands/` — they belong in `Emails/`.
- Never use non-primitive types in email command fields — Hangfire serialization will break.