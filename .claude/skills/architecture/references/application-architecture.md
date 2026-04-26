# Camada: Application

> Orquestra operações de negócio: carrega aggregates, invoca comportamento de domínio, persiste eventos, agenda efeitos colaterais.
> Nunca contém lógica de negócio — isso pertence ao Domain.
> Referencia Domain, Shared e Core. Nunca referencia Infrastructure ou Persistence diretamente.

---

## Estrutura da Camada

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

Definido em `src/Core/Core.Application/Messaging/Commands/ICommandHandler.cs`:

```csharp
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand { }

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse> { }
```

---

## IIdentityApplicationService

Definido em `src/Modules/Identity/Identity.Application/Services/IIdentityApplicationService.cs`:

```csharp
public interface IIdentityApplicationService : IApplicationService<IdentityDbContext> { }
```

Interface marker que vincula o `IApplicationService<TContext>` genérico ao DbContext do módulo. Sempre injete `IIdentityApplicationService` — nunca `IApplicationService<IdentityDbContext>` diretamente.

Expõe:
- `AppendEventsAsync<TAggregate>(aggregate, ct)` — persiste eventos no PostgreSQL, depois publica no RabbitMQ
- `LoadAggregateAsync<TAggregate>(id, ct)` — reidrata o aggregate a partir do event store
- `PublishEventAsync(event, ct)` — publica um evento diretamente no barramento de eventos
- `SchedulePublishAsync(event, scheduledTime, ct)` — agenda um evento atrasado

---

## Anatomia do Handler

### Handler de comando — sem resposta

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

### Handler de comando — com resposta

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

### Quando usar projection vs event store

- **Projection (MongoDB)** → verificações de duplicidade, buscas por campos que não sejam Id (ex.: token, email), listagem
- **Event store (LoadAggregateAsync)** → qualquer operação que muta o estado do aggregate

---

## Anatomia do Validator

Validators são descobertos automaticamente pelo scan do FluentValidation em `ApplicationServiceInstaller` — nenhum registro manual é necessário.

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

**Regras de Validator:**
- `sealed class` herdando de `AbstractValidator<TCommand>`
- Valida apenas o formato do comando — nunca carrega aggregates ou projections
- Validação de estado (ex.: `invite.Status == Pending`) pertence ao Handler, não ao Validator
- Validators podem referenciar enumerações de Domain (`UserRole`, `InviteStatus`) para validação de valor

---

## Handler de Email

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

`IEmailService` não sabe o que é um convite — assunto, corpo e modelo são responsabilidade deste handler. Este handler só é alcançado via Hangfire — nunca chamado diretamente por um handler de negócio.

---

## Handler de Job

Comando e handler de job vivem no mesmo arquivo. Comandos de job são `internal` — eles não são expostos fora de Application.

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

## Anatomia do Projection Handler

Projection handlers atualizam modelos de leitura MongoDB em resposta a Domain Events consumidos via MassTransit.

### Pattern de interface — interface composta por aggregate

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

Definido em `src/Core/Core.Application/EventBus/IEventHandler.cs`:

```csharp
public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    Task Handle(TEvent @event, CancellationToken cancellationToken = default);
}
```

### Registro de projection handler

Projection handlers implementam interfaces compostas e NÃO são capturados pelo scan do Scrutor — eles devem ser registrados manualmente.

Definido em `src/Modules/Identity/Identity.Application/DependencyInjection/ServiceCollectionExtensions.cs`:

```csharp
public static IServiceCollection AddEventInteractors(this IServiceCollection services)
    => services
        .AddScoped<IProjectionUserEventHandler, ProjectionUserEventHandler>()
        .AddScoped<IProjectionUserInviteEventHandler, ProjectionUserInviteEventHandler>();
```

Este método é chamado dentro de `ApplicationServiceInstaller.Install()`.

**Todo novo projection handler deve ser registrado aqui — ele não será descoberto automaticamente.**

---

## O que NUNCA fazer em Application

**Handlers**
- Nunca coloque lógica de negócio em um Handler — regras de negócio pertencem aos métodos do Aggregate.
- Nunca mute estado de aggregate diretamente em um Handler — sempre chame um método de comportamento no aggregate.
- Nunca carregue um aggregate a partir de uma projection — use `LoadAggregateAsync` para qualquer mutação.
- Nunca use uma projection para validação de estado depois de carregar um aggregate — o aggregate é a fonte da verdade.
- Nunca envie email diretamente a partir de um Handler de negócio — sempre enfileire via `IJobSchedulerService`.

**Validators**
- Nunca carregue aggregates ou projections dentro de um Validator — valide apenas o formato do comando.
- Nunca coloque validação de estado em um Validator — regras de estado (ex.: `Status == Pending`) pertencem ao Handler.

**Projection Handlers**
- Nunca lance diretamente a partir do bloco catch de um projection handler — sempre envolva em `InvalidOperationException` para acionar retry do MassTransit.
- Nunca falhe silenciosamente quando `existing is null` em um handler de update — registre warning e retorne (idempotência).
- Nunca esqueça de adicionar um novo projection handler a `AddEventInteractors()` — o Scrutor não vai encontrá-lo.

**Email**
- Nunca passe objetos de domínio ou tipos complexos para comandos de email — apenas primitivos (serialização do Hangfire).
- Nunca chame `IEmailService` diretamente a partir de um Handler de negócio — apenas a partir de `SendInviteEmailHandler`.