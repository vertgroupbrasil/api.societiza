# Feature Implementation Checklist

> Use this checklist every time you implement a new feature end-to-end.
> Follow the order below — never skip layers or implement out of sequence.
> For detailed patterns in each layer, read the corresponding reference file.

---

## Before You Start

- Read `references/domain-architecture.md` if the feature requires a new Aggregate, Event, or Enumeration
- Read `references/application-architecture.md` for Handler, Validator, and Projection Handler patterns
- Read `references/shared-architecture.md` for Command and Response DTO structure
- Read `references/persistence-architecture.md` if the feature adds a new Aggregate to the event store
- Read `references/infrastructure-architecture.md` if the feature requires a new service registration

---

## Step 1 — Domain

### New Aggregate
- [ ] Create `{Module}.Domain/Aggregates/{AggregateName}.cs` inheriting `AggregateRoot`
- [ ] Private constructor
- [ ] Public properties with `private set`
- [ ] `Create()` factory method — raises the creation event via `RaiseEvent<T>()`
- [ ] Behavior methods — each calls `RaiseEvent<T>()`, never assigns state directly
- [ ] `ApplyEvent(IDomainEvent @event) => When(@event as dynamic)`
- [ ] One `private void When(TEvent @event)` per Domain Event

### New Domain Events
- [ ] Add `sealed record` to `{Module}.Domain/Events/DomainEvents.cs`
- [ ] Inherits `Message, IDomainEvent` — in this order
- [ ] All fields are primitives — no SmartEnum, no Domain types
- [ ] `Version` field present in every event
- [ ] SmartEnum fields converted to `string` via `.Name` when raising the event

### New Enumeration
- [ ] Create `{Module}.Domain/Enumerations/{EnumName}.cs`
- [ ] `abstract class` inheriting `SmartEnum<T>`
- [ ] Implicit `string` and `int` conversions
- [ ] `private sealed` subclass per value with stable explicit `int`

### New Projection Model
- [ ] Add `record` to `{Module}.Domain/Projections/ProjectionModel.cs`
- [ ] Implements `IProjectionModel`
- [ ] All fields are primitives — no SmartEnum, no Domain types
- [ ] `IsDeleted` with default `false`

---

## Step 2 — Shared

### New Command
- [ ] Create `{Module}.Shared/Commands/{OperationName}Command.cs`
- [ ] `sealed record` implementing `ICommand` or `ICommand<IdentifierResponse>`
- [ ] All fields are primitives — no Domain types, no SmartEnums

### New Email Command
- [ ] Create `{Module}.Shared/Emails/{OperationName}EmailCommand.cs`
- [ ] `sealed record` implementing `ICommand`
- [ ] All fields are `string` — Hangfire serialization requirement

### New Response DTO
- [ ] Create `{Module}.Shared/Response/{Resource}{Intent}Response.cs`
- [ ] `sealed record` — no inheritance, no interfaces
- [ ] All fields are primitives, `Guid`, `DateTimeOffset`, or nullable variants

---

## Step 3 — Application

### New Validator
- [ ] Create `{Module}.Application/UseCases/Validators/{OperationName}Validator.cs`
- [ ] `sealed class` inheriting `AbstractValidator<TCommand>`
- [ ] Validates command shape only — no aggregate or projection loading
- [ ] State rules (e.g. `Status == Pending`) belong in the Handler, not here

### New Command Handler
- [ ] Create `{Module}.Application/UseCases/Commands/{OperationName}Handler.cs`
- [ ] Implements `ICommandHandler<TCommand>` or `ICommandHandler<TCommand, TResponse>`
- [ ] Use projection (`IIdentityProjection<T>`) for duplicate checks and lookups
- [ ] Use `LoadAggregateAsync` for any mutation
- [ ] Call behavior method on the aggregate — never mutate state directly
- [ ] Call `AppendEventsAsync` after the behavior method
- [ ] Schedule emails via `IJobSchedulerService.Enqueue()` — never send directly

### New Projection Handler
- [ ] Add `IEventHandler<TEvent>` to the composite interface in `ProjectionUserInviteEventHandler.cs`
- [ ] Add `Handle(TEvent @event, ...)` method to the handler class
- [ ] Creation events: call `ReplaceInsertAsync` with a new projection record
- [ ] Update events: `FindAsync` → `existing with { ... }` → `ReplaceInsertAsync`
- [ ] Wrap every `Handle` body in `try/catch` — log error and rethrow as `InvalidOperationException`
- [ ] If `existing is null` in an update handler: log warning and return (idempotency)
- [ ] Register the new handler in `ServiceCollectionExtensions.AddEventInteractors()`

### New Email Handler
- [ ] Create `{Module}.Application/UseCases/Emails/{OperationName}EmailHandler.cs`
- [ ] Implements `ICommandHandler<TEmailCommand>`
- [ ] Calls `IEmailService.SendAsync()` — subject and body defined here, not in `IEmailService`

### New Job Handler
- [ ] Create `{Module}.Application/UseCases/Jobs/{JobName}Job.cs`
- [ ] `Command` and `Handler` in the same file
- [ ] Command is `internal` — not exposed outside Application
- [ ] Use projection to find candidates, event store to load and mutate aggregates
- [ ] Use `continue` in loops — do not fail the entire batch for one aggregate

---

## Step 4 — Persistence

### New Aggregate in the Event Store
- [ ] Add `{AggregateName}StoreEventConfiguration : StoreEventConfiguration<TAggregate>` to `Configurations/`
- [ ] Add `{AggregateName}SnapshotConfiguration : SnapshotConfiguration<TAggregate>` to `Configurations/`
- [ ] Run `dotnet ef migrations add {MigrationName}` to generate the tables

---

## Step 5 — Infrastructure

### New Service (auto-registered)
- [ ] Implement `ITransient` or `IScoped` on the class — Scrutor will auto-register it
- [ ] Define the interface in `{Module}.Application/Services/`
- [ ] Place the implementation in `{Module}.Infrastructure/Services/`

### New Open Generic Registration
- [ ] Add explicit `services.AddScoped(typeof(IFoo<>), typeof(Foo<>))` to `PersistenceServiceInstaller`
- [ ] Never rely on Scrutor for open generics

---

## Step 6 — Web (Consumers)

### New MassTransit Consumer Event

### New Request DTO
- [ ] Create `src/Web/Endpoints/Modules/{Module}/Requests/{Commands|Queries}/{OperationName}Request.cs`
- [ ] Use `[FromRoute]`, `[FromBody]`, or `[FromQuery]` attributes as needed

### New Endpoint
- [ ] Create `src/Web/Endpoints/Modules/{Module}/{OperationName}Endpoint.cs`
- [ ] Inherits `EndpointBaseAsync.WithRequest<TRequest>.WithActionResult` or `.WithActionResult<TResponse>`
- [ ] Decorated with `[ApiVersion("1.0")]`, `[Http{Method}(route)]`, `[ProducesResponseType]`, `[SwaggerOperation]`
- [ ] Pipeline: `Result.Create(request).Map(...).Bind(...).Match(onSuccess, this.HandleFailure)`

---

## Step 6 — Web (Consumers)

### New MassTransit Consumer Event
- [ ] Add `IConsumer<TEvent>` to the existing consumer class for the aggregate (e.g. `ProjectionUserInviteConsumer`)
- [ ] Add `Consume(ConsumeContext<TEvent> context)` method — delegate immediately to the event handler, no logic
- [ ] Add `bus.ConfigureEventReceiveEndpoint<TConsumer, TEvent>(context, MODULE_NAME)` to `ConsumerConfiguration.cs`

---

## Step 7 — Web (Endpoint)

### New Route
- [ ] Add route constant to `src/Web/Endpoints/Routes/{Module}Routes.cs`
- [ ] Add parameter name constant if the route has a `{param:guid}` segment — shared with `[FromRoute(Name = ...)]`

### New Recurring Job
- [ ] Add `jobScheduler.ScheduleRecurring(jobId, cronExpression, new {JobName}Command())` to `RegisterRecurringJobsStartupTask.cs`

### New Module DbContext
- [ ] Add `await MigrateDatabaseAsync<{Module}DbContext>(scope, stoppingToken)` to `MigrateDatabaseStartupTask.cs`

### New Request DTO
- [ ] Create `src/Web/Endpoints/Modules/{Module}/Requests/Commands/{Operation}Request.cs`
- [ ] `sealed class` with public properties and default values
- [ ] Use `[FromRoute(Name = Routes.ConstantName)]` for URL segments — never hardcode the string
- [ ] Use `[FromBody]` implicitly for POST/PUT body fields — no attribute needed
- [ ] Use `[FromQuery]` for query string parameters

### New Endpoint
- [ ] Create `src/Web/Endpoints/Modules/{Module}/{Operation}Endpoint.cs`
- [ ] Inherits `EndpointBaseAsync.WithRequest<TRequest>.WithActionResult` or `.WithActionResult<TResponse>`
- [ ] `[ApiVersion("1.0")]` — mandatory
- [ ] `[Http{Verb}(Routes.Constant)]` — use route constant, never hardcode
- [ ] `[ProducesResponseType]` — one per possible HTTP status code
- [ ] `[SwaggerOperation(Summary, Description, Tags)]` — mandatory
- [ ] Pipeline: `Result.Create(request).Map(...).Bind(...).Match(onSuccess, this.HandleFailure)`
- [ ] Without response body: `.Match(() => Ok(), this.HandleFailure)`
- [ ] With response body: `.Match(Ok, this.HandleFailure)`

---

## What Requires Manual Registration

- Projection handlers → `ServiceCollectionExtensions.AddEventInteractors()`
- MassTransit consumer endpoints → `ConsumerConfiguration.cs`
- Open generic services → `PersistenceServiceInstaller`
- `IIdentityApplicationService` → `ApplicationServiceInstaller` via `TryAddTransient`

## What is Auto-Discovered (no registration needed)

- MediatR handlers → scanned from `Application.AssemblyReference.Assembly`
- FluentValidation validators → scanned from `Application.AssemblyReference.Assembly`
- `ITransient` / `IScoped` services → Scrutor scan in `IdentityModuleInstaller`
- MassTransit consumers → `AddConsumers(AssemblyReference.Assembly)` in `EventBusServiceInstaller`