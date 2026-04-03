# Layer: Application

> Orchestrates business operations: loads aggregates, invokes domain behavior, persists events, schedules side effects.
> Never contains business logic — that belongs in the Domain.
> References Domain, Shared, and Core. Never references Infrastructure or Persistence directly.

---

## Layer Structure

```
Identity.Application/
├── Services/
│   ├── IIdentityApplicationService.cs  → typed gateway to the event store
│   └── IPasswordService.cs             → password hashing interface
├── DependencyInjection/
│   └── ServiceCollectionExtensions.cs  → registers projection handlers (not picked by Scrutor)
└── UseCases/
    ├── Commands/    → ICommandHandler<TCommand> implementations
    ├── Emails/      → email command handlers
    ├── Events/      → projection handlers (MongoDB read model updaters)
    ├── Jobs/        → background job command + handler (same file)
    └── Validators/  → FluentValidation validators for each command
```

---

## ICommandHandler

Defined in `src/Core/Core.Application/Messaging/Commands/ICommandHandler.cs`:

```csharp
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand { }

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse> { }
```

---

## IIdentityApplicationService

Defined in `src/Modules/Identity/Identity.Application/Services/IIdentityApplicationService.cs`:

```csharp
public interface IIdentityApplicationService : IApplicationService<IdentityDbContext> { }
```

Marker interface that binds the generic `IApplicationService<TContext>` to the module's DbContext. Always inject `IIdentityApplicationService` — never `IApplicationService<IdentityDbContext>` directly.

Exposes:
- `AppendEventsAsync<TAggregate>(aggregate, ct)` — persists events to PostgreSQL, then publishes to RabbitMQ
- `LoadAggregateAsync<TAggregate>(id, ct)` — rehydrates aggregate from event store
- `PublishEventAsync(event, ct)` — publishes an event directly to the event bus
- `SchedulePublishAsync(event, scheduledTime, ct)` — schedules a delayed event

---

## Handler Anatomy

### Command Handler — no response

```csharp
// src/Modules/Identity/Identity.Application/UseCases/Commands/RevokeInviteHandler.cs
public sealed class RevokeInviteHandler(
    IIdentityApplicationService applicationService
) : ICommandHandler<RevokeInviteCommand>
{
    public async Task<Result> Handle(RevokeInviteCommand request, CancellationToken cancellationToken)
    {
        // 1. Load aggregate from event store — never from projection
        var inviteResult = await applicationService.LoadAggregateAsync<UserInvite>(
            request.InviteId, cancellationToken);

        if (!inviteResult.IsSuccess)
            return Result.Failure(new NotFoundError(new("Invite.NotFound", "Invite not found")));

        var invite = inviteResult.Value;

        // 2. Validate state rules that require the aggregate (not just the command shape)
        if (invite.Status != InviteStatus.Pending)
            return Result.Failure(new ConflictError(new(
                "Invite.NotRevokable", "Only pending invites can be revoked")));

        // 3. Call behavior method on the aggregate — never mutate state directly
        invite.Revoke();

        // 4. Persist events — inside transaction + publish to RabbitMQ after commit
        await applicationService.AppendEventsAsync(invite, cancellationToken);

        return Result.Success();
    }
}
```

### Command Handler — with response

```csharp
// src/Modules/Identity/Identity.Application/UseCases/Commands/InviteUserHandler.cs
public class InviteUserHandler(
    IIdentityApplicationService applicationService,
    IIdentityProjection<ProjectionModel.UserInvite> inviteProjection,
    IIdentityProjection<ProjectionModel.User> userProjection,
    IJobSchedulerService jobScheduler,
    IOptions<FrontendOptions> frontendOptions
) : ICommandHandler<InviteUserCommand, IdentifierResponse>
{
    public async Task<Result<IdentifierResponse>> Handle(
        InviteUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Use projection (MongoDB) for duplicate checks — fast, no aggregate loading needed
        var pendingInvite = await inviteProjection.FindAsync(
            x => x.Email == request.Email && x.Status == InviteStatus.Pending.Name,
            cancellationToken);

        if (pendingInvite is not null)
            return Result.Failure<IdentifierResponse>(new ConflictError(new(
                "Invite.AlreadyPending", "There is already a pending invite for this email")));

        var existingUser = await userProjection.FindAsync(
            x => x.Email == request.Email, cancellationToken);

        if (existingUser is not null)
            return Result.Failure<IdentifierResponse>(new ConflictError(new(
                "User.AlreadyExists", "A user with this email already exists")));

        // 2. Implicit conversion string → SmartEnum
        UserRole role = request.Role;

        // 3. Raw token goes in the email; only the hash is stored
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        // 4. Create aggregate via factory method
        var invite = UserInvite.Create(
            email: request.Email, role: role,
            accountancyId: request.AccountancyId, tokenHash: tokenHash);

        // 5. Persist
        await applicationService.AppendEventsAsync(invite, cancellationToken);

        // 6. Schedule email via Hangfire — never send directly from a handler
        jobScheduler.Enqueue(new SendInviteEmailCommand(
            ToEmail: request.Email,
            ToName: request.Email,
            InviteLink: $"{frontendOptions.Value.BaseUrl}/auth/accept-invite?token={rawToken}"));

        return Result.Success(new IdentifierResponse(invite.Id));
    }
}
```

### When to use projection vs event store

- **Projection (MongoDB)** → duplicate checks, lookups by non-Id fields (e.g. token, email), listing
- **Event store (LoadAggregateAsync)** → any operation that mutates aggregate state

---

## Validator Anatomy

Validators are discovered automatically by FluentValidation scan in `ApplicationServiceInstaller` — no manual registration needed.

```csharp
// src/Modules/Identity/Identity.Application/UseCases/Validators/InviteUserValidator.cs
public sealed class InviteUserValidator : AbstractValidator<InviteUserCommand>
{
    public InviteUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be valid")
            .MaximumLength(150).WithMessage("Email cannot exceed 150 characters");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required")
            .Must(role => UserRole.TryFromName(role, out _))
            .WithMessage($"Role must be one of: {string.Join(", ", UserRole.List.Select(r => r.Name))}");

        // Conditional rule — AccountancyId required except for SystemAdmin
        RuleFor(x => x.AccountancyId)
            .NotNull().WithMessage("AccountancyId is required")
            .Must(id => id != Guid.Empty).WithMessage("AccountancyId must be a valid GUID")
            .When(x => !UserRole.TryFromName(x.Role, out var role) || role != UserRole.SystemAdmin);
    }
}
```

**Validator rules:**
- `sealed class` inheriting `AbstractValidator<TCommand>`
- Validates only the command shape — never loads aggregates or projections
- State validation (e.g. `invite.Status == Pending`) belongs in the Handler, not the Validator
- Validators may reference Domain enumerations (`UserRole`, `InviteStatus`) for value validation

---

## Email Handler

```csharp
// src/Modules/Identity/Identity.Application/UseCases/Emails/SendInviteEmailHandler.cs
public class SendInviteEmailHandler(IEmailService emailService)
    : ICommandHandler<SendInviteEmailCommand>
{
    public async Task<Result> Handle(
        SendInviteEmailCommand command, CancellationToken cancellationToken)
    {
        await emailService.SendAsync(
            toEmail: command.ToEmail,
            toName: command.ToName,
            subject: "Você foi convidado para o Societiza",
            body: BuildBody(command.ToName, command.InviteLink),
            cancellationToken: cancellationToken);

        return Result.Success();
    }

    private static string BuildBody(string name, string inviteLink) => $"""
        <p>Olá, {name}!</p>
        <p>Você foi convidado para acessar a plataforma <strong>Societiza</strong>.</p>
        <p>Clique no link abaixo para aceitar o convite e definir sua senha:</p>
        <p><a href="{inviteLink}">Aceitar convite</a></p>
        """;
}
```

`IEmailService` does not know what an invite is — subject, body, and template are this handler's responsibility. This handler is only ever reached via Hangfire — never called directly from a business handler.

---

## Job Handler

Job command and handler live in the same file. Job commands are `internal` — they are not exposed outside Application.

```csharp
// src/Modules/Identity/Identity.Application/UseCases/Jobs/ExpireInvitesJob.cs
public sealed record ExpireInvitesCommand() : ICommand;  // internal to Application

internal sealed class ExpireInvitesHandler(
    IIdentityApplicationService applicationService,
    IIdentityProjection<ProjectionModel.UserInvite> inviteProjection,
    ILogger<ExpireInvitesHandler> logger
) : ICommandHandler<ExpireInvitesCommand>
{
    public async Task<Result> Handle(ExpireInvitesCommand request, CancellationToken cancellationToken)
    {
        // 1. Query projection for candidates — efficient, no aggregate loading
        var expiredInvites = await inviteProjection.ListAsync(
            x => x.Status == InviteStatus.Pending.Name && x.ExpiresAt < DateTimeOffset.UtcNow,
            cancellationToken);

        // 2. Load each aggregate and expire
        foreach (var expired in expiredInvites)
        {
            var inviteResult = await applicationService.LoadAggregateAsync<UserInvite>(
                expired.Id, cancellationToken);

            if (!inviteResult.IsSuccess)
            {
                logger.LogWarning("Failed to load invite for expiration: {Id}.", expired.Id);
                continue;  // do not fail the entire batch for one aggregate
            }

            inviteResult.Value.Expire();
            await applicationService.AppendEventsAsync(inviteResult.Value, cancellationToken);
        }

        return Result.Success();
    }
}
```

---

## Projection Handler Anatomy

Projection handlers update MongoDB read models in response to Domain Events consumed via MassTransit.

### Interface pattern — composite interface per aggregate

```csharp
// src/Modules/Identity/Identity.Application/UseCases/Events/ProjectionUserInviteEventHandler.cs

// Composite interface — declares every event this handler processes
public interface IProjectionUserInviteEventHandler :
    IEventHandler<DomainEvents.UserInviteCreated>,
    IEventHandler<DomainEvents.UserInviteResent>,
    IEventHandler<DomainEvents.UserInviteAccepted>,
    IEventHandler<DomainEvents.UserInviteRevoked>,
    IEventHandler<DomainEvents.UserInviteExpired>;

public class ProjectionUserInviteEventHandler(
    IIdentityProjection<ProjectionModel.UserInvite> inviteProjection,
    ILogger<ProjectionUserInviteEventHandler> logger
) : IProjectionUserInviteEventHandler
{
    // Creation event — insert full document
    public async Task Handle(DomainEvents.UserInviteCreated @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await inviteProjection.ReplaceInsertAsync(new(
                @event.Id, @event.Email, @event.Role, @event.AccountancyId,
                @event.TokenHash, @event.Status, @event.ExpiresAt,
                null,            // UsedAt
                @event.CreatedAt,
                null),           // UpdatedAt
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project invite creation: {Id}.", @event.Id);
            throw new InvalidOperationException(...);  // rethrow to trigger MassTransit retry
        }
    }

    // Update events — load existing, apply changes with `with`, upsert
    public async Task Handle(DomainEvents.UserInviteRevoked @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await inviteProjection.FindAsync(x => x.Id == @event.Id, cancellationToken);

            if (existing is null)
            {
                logger.LogWarning("Invite projection not found for revocation: {Id}.", @event.Id);
                return;  // idempotent — do not fail if already processed
            }

            // `with` creates a new record preserving all unchanged fields
            await inviteProjection.ReplaceInsertAsync(existing with
            {
                Status = @event.Status,
                UsedAt = @event.UsedAt,
                UpdatedAt = @event.UpdatedAt
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project invite revocation: {Id}.", @event.Id);
            throw new InvalidOperationException(...);  // rethrow to trigger MassTransit retry
        }
    }

    // Resend, Accepted, Expired — same pattern as Revoked
}
```

### IEventHandler (Core)

Defined in `src/Core/Core.Application/EventBus/IEventHandler.cs`:

```csharp
public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    Task Handle(TEvent @event, CancellationToken cancellationToken = default);
}
```

### Projection handler registration

Projection handlers implement composite interfaces and are NOT picked up by the Scrutor scan — they must be registered manually.

Defined in `src/Modules/Identity/Identity.Application/DependencyInjection/ServiceCollectionExtensions.cs`:

```csharp
public static IServiceCollection AddEventInteractors(this IServiceCollection services)
    => services
        .AddScoped<IProjectionUserEventHandler, ProjectionUserEventHandler>()
        .AddScoped<IProjectionUserInviteEventHandler, ProjectionUserInviteEventHandler>();
```

This method is called inside `ApplicationServiceInstaller.Install()`.

**Every new projection handler must be registered here — it will not be auto-discovered.**

---

## What to NEVER do in Application

**Handlers**
- Never place business logic in a Handler — business rules belong in Aggregate methods.
- Never mutate aggregate state directly in a Handler — always call a behavior method on the aggregate.
- Never load an aggregate from a projection — use `LoadAggregateAsync` for any mutation.
- Never use a projection for state validation after loading an aggregate — the aggregate is the source of truth.
- Never send email directly from a business Handler — always enqueue via `IJobSchedulerService`.

**Validators**
- Never load aggregates or projections inside a Validator — validate only the command shape.
- Never put state validation in a Validator — state rules (e.g. `Status == Pending`) belong in the Handler.

**Projection Handlers**
- Never throw directly from a projection handler catch block — always wrap in `InvalidOperationException` to trigger MassTransit retry.
- Never fail silently when `existing is null` in an update handler — log a warning and return (idempotency).
- Never forget to add a new projection handler to `AddEventInteractors()` — Scrutor will not find it.

**Email**
- Never pass domain objects or complex types to email commands — primitives only (Hangfire serialization).
- Never call `IEmailService` directly from a business Handler — only from `SendInviteEmailHandler`.