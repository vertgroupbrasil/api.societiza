# Layer: Infrastructure

> Orchestrates all module services and connects all layers together.
> Contains service implementations, ServiceInstallers, and the ModuleInstaller entry point.
> The only layer that knows all other layers within the module.

---

## Responsibility Boundary

- **Infrastructure** owns: `IModuleInstaller`, `IServiceInstaller` implementations, `IdentityApplicationService`, `JwtTokenService`, `PasswordService`
- **Infrastructure** does NOT own: business logic, EF configurations, MongoDB contexts, MassTransit consumers (those live in `src/Web/`)

---

## Module Entry Point — IModuleInstaller

Every module has a single entry point that orchestrates all service registration.

Defined in `src/Modules/Identity/Identity.Infrastructure/IdentityModuleInstaller.cs`:

```csharp
public sealed class IdentityModuleInstaller : IModuleInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        // Discovers and runs all IServiceInstaller implementations in this assembly
        services.InstallServicesFromAssemblies(configuration, AssemblyReference.Assembly);

        // Scrutor scan — auto-registers classes implementing ITransient or IScoped
        services
            .AddTransientAsMatchingInterfaces(AssemblyReference.Assembly)
            .AddScopedAsMatchingInterfaces(AssemblyReference.Assembly);

        // Same Scrutor scan for the Persistence assembly
        services
            .AddTransientAsMatchingInterfaces(Persistence.AssemblyReference.Assembly)
            .AddScopedAsMatchingInterfaces(Persistence.AssemblyReference.Assembly);
    }
}
```

`InstallServicesFromAssemblies` uses reflection to find all `IServiceInstaller` implementations and calls `Install()` on each. The Scrutor scan (`AddTransientAsMatchingInterfaces`) automatically registers any class implementing `ITransient` or `IScoped` — this is why `JwtTokenService` and `PasswordService` require no explicit registration.

---

## The Three ServiceInstallers

Each installer has a single, well-defined responsibility. Never merge them.

### 1. ApplicationServiceInstaller

Defined in `src/Modules/Identity/Identity.Infrastructure/ServiceInstallers/ApplicationServiceInstaller.cs`:

```csharp
internal sealed class ApplicationServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.Tap(services.TryAddTransient<IIdentityApplicationService, IdentityApplicationService>);
        services.AddEventInteractors();

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(Application.AssemblyReference.Assembly);
            config.AddOpenBehavior(typeof(RequestLoggingPipelineBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });

        services.AddAutoMapper(Application.AssemblyReference.Assembly);
        services.AddValidatorsFromAssembly(Application.AssemblyReference.Assembly);
    }
}
```

Registers: MediatR with both pipeline behaviors (`RequestLogging` and `Validation`), FluentValidation scan, AutoMapper, and `IIdentityApplicationService`. `TryAddTransient` prevents duplicate registration if another module already registered the service.

### 2. InfrastructureServiceInstaller

Defined in `src/Modules/Identity/Identity.Infrastructure/ServiceInstallers/InfrastructureServiceInstaller.cs`:

```csharp
internal sealed class InfrastructureServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureOptions<JwtOptionsConfiguration>();
        services.TryAddTransient<IEventBus, EventBus>();
    }
}
```

Registers: `JwtOptions` via `IConfigureOptions<T>` (ASP.NET Core options pattern), and `IEventBus` which wraps MassTransit's `IPublishEndpoint`.

### 3. PersistenceServiceInstaller

Defined in `src/Modules/Identity/Identity.Infrastructure/ServiceInstallers/PersistenceServiceInstaller.cs`:

```csharp
internal sealed class PersistenceServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        // Open generics must be registered explicitly — Scrutor scan does not handle them
        services.AddScoped(typeof(IIdentityProjection<>), typeof(IdentityProjection<>));
        services.AddScoped(typeof(IEventStore<IdentityDbContext>), typeof(EventStore<IdentityDbContext>));
        services.AddScoped(typeof(IUnitOfWork<IdentityDbContext>), typeof(UnitOfWork<IdentityDbContext>));

        // MongoDB context — connection string from configuration
        services.AddScoped<IIdentityProjectionDbContext>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            string connectionString = configuration
                .GetSection("Projections")
                .GetValue<string>("Identity");
            return new IdentityProjectionDbContext(connectionString);
        });

        // EF Core — PostgreSQL with migration history in module schema
        services.AddDbContext<IdentityDbContext>((provider, builder) =>
        {
            ConnectionStringOptions connectionString = provider
                .GetService<IOptions<ConnectionStringOptions>>()!.Value;
            builder.UseNpgsql(
                connectionString: connectionString,
                dbContextOptionsBuilder => dbContextOptionsBuilder
                    .WithMigrationHistoryTableInSchema(Schemas.Identity));
        });
    }
}
```

Open generic types (`IIdentityProjection<>`, `IEventStore<>`, `IUnitOfWork<>`) must always be registered explicitly — the Scrutor scan does not handle open generics.

---

## Scrutor Scan — Auto-registration Rules

Classes are auto-registered if they implement `ITransient` or `IScoped`. No manual registration needed.

```csharp
// Auto-registered via ITransient — no explicit registration required
public class PasswordService : IPasswordService, ITransient { ... }
```

The Scrutor extension in Core handles the registration:

```csharp
// src/Core/Core.Infrastructure/Extensions/ServiceCollectionExtensions.cs
public static IServiceCollection AddTransientAsMatchingInterfaces(
    this IServiceCollection services, Assembly assembly) =>
    services.Scan(scan =>
        scan.FromAssemblies(assembly)
            .AddClasses(filter => filter.AssignableTo<ITransient>(), false)
            .UsingRegistrationStrategy(RegistrationStrategy.Throw)
            .AsMatchingInterface()
            .WithTransientLifetime());
```

`RegistrationStrategy.Throw` means duplicate registrations will throw — always use `TryAddTransient` when there is risk of duplicate registration across modules.

---

## IdentityApplicationService

Defined in `src/Modules/Identity/Identity.Infrastructure/Services/IdentityApplicationService.cs`:

```csharp
public class IdentityApplicationService(
    IEventStore<IdentityDbContext> eventStore,
    IEventBus eventBusGateway,
    IUnitOfWork<IdentityDbContext> unitOfWork)
    : ApplicationService<IdentityDbContext>(eventStore, eventBusGateway, unitOfWork),
      IIdentityApplicationService
{
}
```

This is an empty wrapper — all real logic lives in `ApplicationService<TContext>` in Core. Its only purpose is to bind the correct `DbContext` type to the module, enabling typed injection without cross-module DbContext access.

The interface `IIdentityApplicationService` is a marker that specializes `IApplicationService<IdentityDbContext>`:

```csharp
// src/Modules/Identity/Identity.Application/Services/IIdentityApplicationService.cs
public interface IIdentityApplicationService : IApplicationService<IdentityDbContext> { }
```

---

## Module File Map

```
Identity.Infrastructure/
├── IdentityModuleInstaller.cs              → module entry point, orchestrates all installers
├── ServiceInstallers/
│   ├── ApplicationServiceInstaller.cs      → MediatR, FluentValidation, IIdentityApplicationService
│   ├── InfrastructureServiceInstaller.cs   → JwtOptions, IEventBus
│   └── PersistenceServiceInstaller.cs      → EF Core, MongoDB, open generic registrations
└── Services/
    ├── IdentityApplicationService.cs       → empty wrapper binding module DbContext to Core service
    ├── JwtTokenService.cs                  → JWT generation/validation (auto-registered via ITransient)
    └── PasswordService.cs                  → PBKDF2 hash + password generation (auto-registered via ITransient)
```

---

## What to NEVER do in Infrastructure

**Service registration**
- Never register open generic types via Scrutor scan — always register them explicitly in `PersistenceServiceInstaller`.
- Never add a new `IServiceInstaller` for unrelated concerns — keep the three installers focused on their single responsibility.
- Never register `IIdentityApplicationService` with `AddTransient` — always use `TryAddTransient` to prevent duplicate registration errors.

**Scrutor scan**
- Never implement `ITransient` or `IScoped` on a class that needs constructor arguments resolved from configuration — use explicit registration instead.
- Never rely on Scrutor to register open generics — it does not support them.

**Layer boundaries**
- Never place business logic in Infrastructure services.
- Never reference `Identity.Persistence` directly from a Handler — always go through `IIdentityApplicationService` or `IIdentityProjection<T>`.