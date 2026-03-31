# Layer: Web

> The Web layer is the ASP.NET Core host — entry point for HTTP requests and event bus consumers.
> Contains Endpoints, Request DTOs, Routes, and MassTransit Consumers.
> Never contains business logic — delegates immediately to Application via MediatR or event handlers.

---

## Responsibility Boundary

- **Web** owns: Endpoints, Request DTOs, Routes, MassTransit Consumers, `ConsumerConfiguration`, `EventBusServiceInstaller`
- **Web** does NOT own: business logic, domain types, projection logic (consumers delegate to Application handlers)

---

## Endpoints

### Base Class

All endpoints inherit from `EndpointBaseAsync` (Ardalis):

```csharp
// Without response body
EndpointBaseAsync.WithRequest<TRequest>.WithActionResult

// With response body
EndpointBaseAsync.WithRequest<TRequest>.WithActionResult<TResponse>
```

The only method to implement is `HandleAsync(TRequest request, CancellationToken cancellationToken)`.

### Endpoint Anatomy

```csharp
// src/Web/Endpoints/Modules/Identity/RevokeInviteEndpoint.cs
public sealed class RevokeInviteEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<RevokeInviteRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpPost(IdentityRoutes.RevokeInvite)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [SwaggerOperation(
        Summary = "Revoke invite",
        Description = "Revokes a pending invite. Only invites with Pending status can be revoked.",
        Tags = [Tags.Identity])]
    public override async Task<ActionResult> HandleAsync(
        RevokeInviteRequest request, CancellationToken cancellationToken)
        => await Result.Create(request)
            .Map(request => new RevokeInviteCommand(InviteId: request.InviteId))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(() => Ok(), this.HandleFailure);
}
```

### Railway-Oriented Pipeline

Every endpoint follows the same four-step pipeline:

```csharp
await Result.Create(request)                         // 1. Wrap request in Result<TRequest>
    .Map(request => new XCommand(...))               // 2. Transform to Command — sync, cannot fail
    .Bind(command => sender.Send(command, ct))       // 3. Dispatch via MediatR → returns Result
    .Match(onSuccess, this.HandleFailure);            // 4. Unwrap to ActionResult
```

**Without response body** — `Match(() => Ok(), this.HandleFailure)`

**With response body** — `Match(Ok, this.HandleFailure)` — `Ok` receives the value directly

### Mandatory Decorators

Every endpoint requires all of these:

- `[ApiVersion("1.0")]` — versioning via URL segment
- `[Http{Verb}(IdentityRoutes.X)]` — verb and route constant
- `[ProducesResponseType(...)]` — one per possible HTTP status code
- `[SwaggerOperation(Summary, Description, Tags)]` — Swagger documentation

---

## HandleFailure — Error to HTTP Translation

Defined in `src/Core/Core.Endpoints/Extensions/EndpointBaseExtensions.cs`:

```csharp
public static ActionResult HandleFailure(this EndpointBase endpoint, Result result) =>
    result switch
    {
        { IsSuccess: true }                          => throw new InvalidOperationException(),
        IValidationResult vr                         => endpoint.BadRequest(ProblemDetails(400, vr.Errors)),
        var r when r.Error is NotFoundError          => endpoint.NotFound(ProblemDetails(404, r.Error)),
        var r when r.Error is ConflictError          => endpoint.Conflict(ProblemDetails(409, r.Error)),
        var r                                        => endpoint.BadRequest(ProblemDetails(400, r.Error))
    };
```

Error type to HTTP status mapping:

- `IValidationResult` (multiple errors) → `400 Bad Request` with `errors` array
- `NotFoundError` → `404 Not Found`
- `ConflictError` → `409 Conflict`
- Any other `Error` → `400 Bad Request`

Response body always follows RFC 7807 `ProblemDetails`: `title`, `type` (= `Error.Code`), `detail` (= `Error.Message`), `status`, and `errors` (validation only).

---

## Request DTOs

Defined in `src/Web/Endpoints/Modules/{Module}/Requests/{Commands|Queries}/`.

```csharp
// FromBody (implicit for POST — no attribute needed)
// src/Web/Endpoints/Modules/Identity/Requests/Commands/InviteUserRequest.cs
public sealed class InviteUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid? AccountancyId { get; set; }
}

// FromRoute — Id from URL segment
// src/Web/Endpoints/Modules/Identity/Requests/Commands/RevokeInviteRequest.cs
public sealed class RevokeInviteRequest
{
    [FromRoute(Name = IdentityRoutes.InviteId)]   // references the route constant — no magic strings
    public Guid InviteId { get; set; }
}
```

**Request DTO rules:**
- `sealed class` with `public` properties and default values
- Use `[FromRoute(Name = Routes.ConstantName)]` for URL segments — always reference the route constant, never hardcode the string
- Use `[FromBody]` implicitly for POST/PUT body fields — no attribute needed
- Use `[FromQuery]` for query string parameters
- Request DTOs are HTTP concerns only — never pass them to handlers directly, always map to a Command/Query

---

## Routes

Defined in `src/Web/Endpoints/Routes/{Module}Routes.cs`:

```csharp
// src/Web/Endpoints/Routes/IdentityRoutes.cs
internal static class IdentityRoutes
{
    internal const string BaseUri = "v{version:apiVersion}/identity";
    internal const string InviteId = "inviteId";   // shared between route template and [FromRoute]

    internal const string InviteUser   = $"{BaseUri}/invite";
    internal const string AcceptInvite = $"{BaseUri}/invite/accept";
    internal const string ResendInvite = $"{BaseUri}/invite/{{{InviteId}:guid}}/resend";
    internal const string RevokeInvite = $"{BaseUri}/invite/{{{InviteId}:guid}}/revoke";
}
```

**Route rules:**
- `internal static class` — one file per module
- `BaseUri` always includes `v{version:apiVersion}/{module}` — version segment is mandatory
- Route parameters use `{constantName:guid}` constraint — enforces format at routing level, invalid requests never reach the handler
- Parameter name constants (e.g. `InviteId`) are shared between the route template and `[FromRoute(Name = ...)]`
- All routes are prefixed with `/api` by the host configuration — do not include `/api` in the constant

Resulting URLs: `POST /api/v1/identity/invite`, `POST /api/v1/identity/invite/{guid}/revoke`

---

## MassTransit Consumers

### Consumer Structure

One consumer class per aggregate. Each consumer implements `IConsumer<T>` for every event of that aggregate and delegates immediately to the Application projection handler.

```csharp
// src/Web/ServiceInstallers/EventBus/Consumers/IdentityModule/ProjectionUserInviteConsumer.cs
public class ProjectionUserInviteConsumer(IProjectionUserInviteEventHandler eventHandler)
    : IConsumer<DomainEvents.UserInviteCreated>,
      IConsumer<DomainEvents.UserInviteResent>,
      IConsumer<DomainEvents.UserInviteAccepted>,
      IConsumer<DomainEvents.UserInviteRevoked>,
      IConsumer<DomainEvents.UserInviteExpired>
{
    public Task Consume(ConsumeContext<DomainEvents.UserInviteCreated> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    // Same pattern for every other event
}
```

Consumers contain no logic — they exist only because MassTransit requires a concrete `IConsumer<T>` class. All projection logic lives in Application.

### ConsumerConfiguration

One `ConsumerConfiguration` per module. Registers one queue per `<TConsumer, TEvent>` pair.

```csharp
// src/Web/ServiceInstallers/EventBus/Consumers/IdentityModule/ConsumerConfiguration.cs
internal sealed class ConsumerConfiguration : IEventReceiveEndpointConfiguration
{
    private const string MODULE_NAME = "identity";

    public void AddEventReceiveEndpoints(
        IRabbitMqBusFactoryConfigurator bus, IRegistrationContext context)
    {
        bus.ConfigureEventReceiveEndpoint<ProjectionUserConsumer,
            DomainEvents.UserRegistrationCompleted>(context, MODULE_NAME);

        bus.ConfigureEventReceiveEndpoint<ProjectionUserInviteConsumer,
            DomainEvents.UserInviteCreated>(context, MODULE_NAME);

        bus.ConfigureEventReceiveEndpoint<ProjectionUserInviteConsumer,
            DomainEvents.UserInviteRevoked>(context, MODULE_NAME);

        // One call per event — every event gets its own dedicated queue
    }
}
```

Queue name convention — generated automatically by the extension:

```
webbff.{module}.{consumer-kebab-case}.{event-kebab-case}

Examples:
webbff.identity.projection-user-consumer.user-registration-completed
webbff.identity.projection-user-invite-consumer.user-invite-created
webbff.identity.projection-user-invite-consumer.user-invite-revoked
```

`ConsumerConfiguration` is discovered automatically via reflection at startup — implementing `IEventReceiveEndpointConfiguration` is sufficient for registration.

### Auto-Discovery

- **Consumer classes** → discovered by `AddConsumers(AssemblyReference.Assembly)` — no manual DI registration needed
- **ConsumerConfiguration** → discovered by `InstanceFactory.CreateFromAssemblies<IEventReceiveEndpointConfiguration>` — no manual registration needed

---

## Startup Tasks

Startup tasks are `BackgroundService` implementations that run once at application startup. Registered in `src/Web/ServiceInstallers/StartupTasks/StartupTasksServiceInstaller.cs`.

### MigrateDatabaseStartupTask

Defined in `src/Web/StartupTasks/MigrateDatabaseStartupTask.cs`:

```csharp
internal sealed class MigrateDatabaseStartupTask(IServiceProvider provider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using IServiceScope scope = provider.CreateScope();

        await MigrateDatabaseAsync<AccountancyDbContext>(scope, stoppingToken);
        await MigrateDatabaseAsync<IdentityDbContext>(scope, stoppingToken);
        await MigrateDatabaseAsync<WorkflowTemplateDbContext>(scope, stoppingToken);
    }

    private static async Task MigrateDatabaseAsync<TDbContext>(IServiceScope scope, CancellationToken cancellationToken)
        where TDbContext : DbContext
    {
        TDbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        if (dbContext.Database.GetMigrations().Any())
            await dbContext.Database.MigrateAsync(cancellationToken);
        else
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }
}
```

**Every new module `DbContext` must be added here** — otherwise migrations will not run on startup.

### RegisterRecurringJobsStartupTask

Defined in `src/Web/StartupTasks/RegisterRecurringJobsStartupTask.cs`:

```csharp
internal sealed class RegisterRecurringJobsStartupTask(IServiceProvider provider) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using IServiceScope scope = provider.CreateScope();
        var jobScheduler = scope.ServiceProvider.GetRequiredService<IJobSchedulerService>();

        jobScheduler.ScheduleRecurring("expire-pending-invites", "0 * * * *", new ExpireInvitesCommand());

        return Task.CompletedTask;
    }
}
```

All recurring Hangfire jobs are registered here. The cron expression `"0 * * * *"` runs every hour. **Every new recurring job must be registered here** — never registered ad hoc from a handler or service.

### Registration

Defined in `src/Web/ServiceInstallers/StartupTasks/StartupTasksServiceInstaller.cs`:

```csharp
internal sealed class StartupTasksServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHostedService<MigrateDatabaseStartupTask>();
        services.AddHostedService<RegisterRecurringJobsStartupTask>();
    }
}
```

---

## Web File Map

```
src/Web/
├── Endpoints/
│   ├── Routes/
│   │   └── {Module}Routes.cs                          → route constants per module
│   └── Modules/{Module}/
│       ├── Requests/
│       │   ├── Commands/{Operation}Request.cs          → HTTP input DTOs
│       │   └── Queries/{Operation}Request.cs
│       └── {Operation}Endpoint.cs                      → one file per endpoint
├── StartupTasks/
│   ├── MigrateDatabaseStartupTask.cs                  → runs EF migrations on startup
│   └── RegisterRecurringJobsStartupTask.cs            → registers Hangfire recurring jobs
└── ServiceInstallers/
    ├── StartupTasks/StartupTasksServiceInstaller.cs   → registers both hosted services
    └── EventBus/
        ├── EventBusServiceInstaller.cs                → MassTransit full assembly
        ├── Extensions/
        │   └── RabbitMqBusFactoryConfiguratorExtensions.cs → queue name convention
        └── Consumers/{Module}Module/
            ├── Projection{Aggregate}Consumer.cs       → IConsumer<T> per aggregate
            └── ConsumerConfiguration.cs               → queue registration per module
```

---

## What to NEVER do in Web

**Endpoints**
- Never place business logic in an endpoint — map to Command/Query and delegate via `sender.Send()`.
- Never pass a Request DTO directly to a handler — always map to a Command or Query in `.Map()`.
- Never skip `[ProducesResponseType]` decorators — document every possible status code.
- Never hardcode route strings — always use constants from `{Module}Routes.cs`.

**Routes**
- Never include `/api` in route constants — it is added by host configuration.
- Never hardcode route parameter names in `[FromRoute(Name = "...")]` — always reference the route constant.

**Consumers**
- Never place projection logic inside a consumer — delegate immediately to the Application event handler.
- Never forget to add a new event to `ConsumerConfiguration` — the queue will not exist in RabbitMQ without it.
- Never add a new `IConsumer<T>` without a corresponding entry in `ConsumerConfiguration`.