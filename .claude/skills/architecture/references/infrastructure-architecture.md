# Camada: Infrastructure

> Orquestra todos os serviços do módulo e conecta todas as camadas.
> Contém implementações de serviços, ServiceInstallers e o ponto de entrada ModuleInstaller.
> A única camada que conhece todas as outras camadas dentro do módulo.

---

## Fronteira de Responsabilidade

- **Infrastructure** é dona de: `IModuleInstaller`, implementações de `IServiceInstaller`, `IdentityApplicationService`, `JwtTokenService`, `PasswordService`
- **Infrastructure** NÃO é dona de: lógica de negócio, configurações EF, contexts MongoDB, consumers MassTransit (eles vivem em `src/Web/`)

---

## Ponto de Entrada do Módulo — IModuleInstaller

Todo módulo tem um único ponto de entrada que orquestra todo o registro de serviços.

Definido em `src/Modules/Identity/Identity.Infrastructure/IdentityModuleInstaller.cs`:

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

`InstallServicesFromAssemblies` usa reflection para encontrar todas as implementações de `IServiceInstaller` e chama `Install()` em cada uma. O scan do Scrutor (`AddTransientAsMatchingInterfaces`) registra automaticamente qualquer classe que implemente `ITransient` ou `IScoped` — por isso `JwtTokenService` e `PasswordService` não exigem registro explícito.

---

## Os Três ServiceInstallers

Cada installer tem uma responsabilidade única e bem definida. Nunca os una.

### 1. ApplicationServiceInstaller

Definido em `src/Modules/Identity/Identity.Infrastructure/ServiceInstallers/ApplicationServiceInstaller.cs`:

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

Registra: MediatR com ambos os pipeline behaviors (`RequestLogging` e `Validation`), scan do FluentValidation, AutoMapper e `IIdentityApplicationService`. `TryAddTransient` evita registro duplicado se outro módulo já registrou o serviço.

### 2. InfrastructureServiceInstaller

Definido em `src/Modules/Identity/Identity.Infrastructure/ServiceInstallers/InfrastructureServiceInstaller.cs`:

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

Registra: `JwtOptions` via `IConfigureOptions<T>` (pattern de options do ASP.NET Core), e `IEventBus`, que encapsula o `IPublishEndpoint` do MassTransit.

### 3. PersistenceServiceInstaller

Definido em `src/Modules/Identity/Identity.Infrastructure/ServiceInstallers/PersistenceServiceInstaller.cs`:

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

Tipos genéricos abertos (`IIdentityProjection<>`, `IEventStore<>`, `IUnitOfWork<>`) devem sempre ser registrados explicitamente — o scan do Scrutor não lida com genéricos abertos.

---

## Scan do Scrutor — Regras de Registro Automático

Classes são registradas automaticamente se implementarem `ITransient` ou `IScoped`. Nenhum registro manual é necessário.

```csharp
// Auto-registered via ITransient — no explicit registration required
public class PasswordService : IPasswordService, ITransient { ... }
```

A extension do Scrutor em Core cuida do registro:

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

`RegistrationStrategy.Throw` significa que registros duplicados lançarão erro — sempre use `TryAddTransient` quando houver risco de registro duplicado entre módulos.

---

## IdentityApplicationService

Definido em `src/Modules/Identity/Identity.Infrastructure/Services/IdentityApplicationService.cs`:

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

Este é um wrapper vazio — toda lógica real vive em `ApplicationService<TContext>` em Core. Seu único propósito é vincular o tipo correto de `DbContext` ao módulo, permitindo injeção tipada sem acesso cross-module a DbContext.

A interface `IIdentityApplicationService` é um marker que especializa `IApplicationService<IdentityDbContext>`:

```csharp
// src/Modules/Identity/Identity.Application/Services/IIdentityApplicationService.cs
public interface IIdentityApplicationService : IApplicationService<IdentityDbContext> { }
```

---

## Mapa de Arquivos do Módulo

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

## O que NUNCA fazer em Infrastructure

**Registro de serviços**
- Nunca registre tipos genéricos abertos via scan do Scrutor — sempre registre explicitamente em `PersistenceServiceInstaller`.
- Nunca adicione um novo `IServiceInstaller` para preocupações não relacionadas — mantenha os três installers focados em sua responsabilidade única.
- Nunca registre `IIdentityApplicationService` com `AddTransient` — sempre use `TryAddTransient` para evitar erros de registro duplicado.

**Scan do Scrutor**
- Nunca implemente `ITransient` ou `IScoped` em uma classe que precisa de argumentos de construtor resolvidos a partir de configuração — use registro explícito.
- Nunca dependa do Scrutor para registrar genéricos abertos — ele não oferece suporte a eles.

**Fronteiras de camada**
- Nunca coloque lógica de negócio em serviços de Infrastructure.
- Nunca referencie `Identity.Persistence` diretamente a partir de um Handler — sempre passe por `IIdentityApplicationService` ou `IIdentityProjection<T>`.