# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Context Documents

Always import the following documents at the start of every conversation. They are the canonical references for the project and domain:

- [`docs/societiza-context.md`](docs/societiza-context.md) — Product context, business rules, glossary, and tech stack overview.
- [`docs/domain-model.md`](docs/domain-model.md) — Canonical domain model: aggregates, entities, value objects, domain events, projection models and enumerations.

---

## Build & Run Commands

### Local Development (Docker)

```bash
# Start infrastructure (PostgreSQL, MongoDB, RabbitMQ)
docker-compose -f Docker/docker-compose.Infrastructure.Development.yaml up -d

# Build Docker image
docker build --no-cache -t societiza-api .

# Run API container
docker run --rm -p 5258:8080 --network societiza-dev --env-file .env.local societiza-api
```

- API: `http://localhost:5258/api`
- Swagger: `http://localhost:5258/api/swagger`

### .NET CLI

```bash
# Build solution
dotnet build Societiza.sln

# Run the web project
dotnet run --project src/Web/Web.csproj

# Run tests
dotnet test Societiza.sln

# Run a single test project
dotnet test tests/<TestProjectName>/

# Add EF migration
dotnet ef migrations add <MigrationName> \
  --project src/Modules/<Module>/<Module>.Persistence \
  --startup-project src/Web \
  --context <Module>DbContext

# Apply migrations to the database
dotnet ef database update \
  --project src/Modules/<Module>/<Module>.Persistence \
  --startup-project src/Web \
  --context <Module>DbContext

# Available contexts:
#   Accountancy      → AccountancyDbContext
#   Identity         → IdentityDbContext
#   WorkflowTemplate → WorkflowTemplateDbContext
```

### Environment Setup

Copy `.env.example` to `.env.local` and configure connection strings before running.

## Architecture

This project is built as an **Event-Driven Modular Monolith** using **DDD**, **CQRS** and **Clean Architecture** principles.

### Solution Structure

```
src/
├── Core/          # Cross-cutting infrastructure (shared by all modules)
├── Modules/       # Feature modules (Accountancy, Identity, WorkflowTemplate)
└── Web/           # ASP.NET Core host (entry point)
tests/             # All test projects
```

Each module and Core has 5 layers: `Domain → Application → Infrastructure → Persistence → Shared`.

### Data Flow

```
HTTP Request
  → Web/Endpoints (Ardalis API Endpoints)
  → MediatR (CQRS handler)
  → Application layer (use cases, validation)
  → Domain logic
  → EF Core → PostgreSQL (writes)
  → Domain Events → MassTransit/RabbitMQ
  → Event Handlers → MongoDB projections (reads)
```

### Key Patterns

- **CQRS via MediatR**: Commands mutate state; queries read from MongoDB projections. Handlers implement `ICommandHandler<TCommand, TResponse>` or `IQueryHandler<TQuery, TResponse>` — abstractions over MediatR's `IRequestHandler<>` defined in `Core.Application`. Never implement `IRequestHandler` directly.
- **Ardalis API Endpoints**: One class per route in `src/Web/Endpoints/Modules/{Module}/`.
- **Service Registration**: Modules registram serviços via `IServiceInstaller` (descoberto por reflexão em `InstallModulesFromAssemblies()`). Core registra via `InfrastructureServiceInstaller` com registros explícitos — sem Scrutor scan. `Program.cs` passa os assemblies manualmente para `InstallServicesFromAssemblies()`.
- **Module Contracts**: The `*.Shared` project in each module holds commands/queries/responses used across module boundaries.
- **Unit of Work + Repository**: Persistence abstractions wrap EF Core; transactions commit via `IUnitOfWork`.
- **MassTransit**: Abstracts the event bus (RabbitMQ transport in all environments). Consumers are registered explicitly via `ConsumerConfiguration` classes in `src/Web/ServiceInstallers/EventBus/Consumers/{Module}/`. Each module has its own consumer classes that delegate to projection handlers in `{Module}.Persistence`.
- **Background Jobs via Hangfire**: Operações assíncronas (ex: envio de email) usam `IJobSchedulerService.Enqueue(command)`. O `MediatorHangfireBridge` despacha o command via MediatR. Jobs são persistidos no PostgreSQL com retry automático.
- **StartupTasks**: Tarefas de inicialização implementam `IStartupTask` e são executadas durante o startup da aplicação. Usadas para registrar Hangfire recurring jobs. Criar em `src/Web/StartupTasks/` e registrar via `StartupTasksServiceInstaller`. Exemplo: `RegisterRecurringJobsStartupTask` registra o job `"expire-pending-invites"` com cron `"0 * * * *"`.

### Databases

| Store | Purpose | Default connection target |
|---|---|---|
| PostgreSQL | Transactional data, Hangfire jobs | `societiza-postgres-dev:5432` |
| MongoDB | Read projections (per module) | `societiza-mongodb-dev:27017` |
| RabbitMQ | Event bus transport | `societiza-rabbitmq-dev:5672` |

### Adding a New Endpoint

1. Create `{Operation}Endpoint.cs` in `src/Web/Endpoints/Modules/{Module}/` and register the route constant in `{Module}Routes.cs` in the same folder.
2. Create the MediatR command/query + handler in the `{Module}.Application` layer. Handlers implement `ICommandHandler<TCommand, TResponse>` or `IQueryHandler<TQuery, TResponse>`.
3. Add validators in `{Module}.Application` — MediatR pipeline automatically runs `ValidationBehavior`.
4. If a new read model is needed, add projection handler in `{Module}.Persistence`.

### API Base Path

All routes are prefixed with `/api` (configured via `PathBase` middleware). API versioning uses URL segments (e.g. `/api/v1/`).

### Logging & Observability

Serilog is configured for structured logging. `CorrelationId` middleware injects a correlation ID into all requests. `RequestTimestampMiddleware` stamps request timing.

## Email

- **Provider**: Brevo via SMTP relay — no SDK or external package. Chosen for its generous free tier (300 emails/day) and no verified domain requirement initially.
- **Integration**: native `System.Net.Mail.SmtpClient` only; do not add external email packages.
- **Development**: use Brevo SMTP with a personal email as both sender and recipient for testing.
- **Production**: Brevo SMTP relay at `smtp-relay.brevo.com`, port `587`, SSL enabled.
- **Future**: when `societiza.com.br` is available, update `FromEmail` to `noreply@societiza.com.br`.
- **Security**: the Brevo SMTP Key must **never** be committed. Store it in `.env.local` (already in `.gitignore`).

### Padrão de uso

`IEmailService` é uma abstração de entrega genérica com um único método `SendAsync(toEmail, toName, subject, body, ct)`. Ele **não conhece conceitos de domínio** como "convite" ou "boas-vindas" — isso é responsabilidade de cada módulo.

**Nunca injete `IEmailService` diretamente em handlers de negócio.** O envio deve sempre ser assíncrono via Hangfire:

1. Cada módulo cria seus próprios commands de email em `{Module}.Shared/Emails/` (command record) e `{Module}.Application/UseCases/Emails/` (handler)
2. Os handlers desses commands injetam `IEmailService`, constroem subject e body HTML, e chamam `SendAsync`
3. Handlers de negócio injetam `IJobSchedulerService` e enfileiram: `_jobScheduler.Enqueue(new SendXxxEmailCommand(...))`

```
// Estrutura por módulo
{Module}.Shared/
└── Emails/
    └── Send{Action}EmailCommand.cs        // sealed record, apenas primitivos (string, Guid)

{Module}.Application/
└── UseCases/Emails/
    └── Send{Action}EmailCommandHandler.cs // injeta IEmailService, constrói subject/body
```

O command fica em `Shared` para ser referenciável sem acoplar à camada Application. Commands de email devem conter **apenas tipos primitivos** nos parâmetros para garantir serialização pelo Hangfire.
