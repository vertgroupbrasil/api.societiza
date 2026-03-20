# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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
dotnet test src/tests/<TestProjectName>/

# Add EF migration (from Web project directory)
dotnet ef migrations add <MigrationName> --project src/Modules/<Module>/<Module>.Persistence --startup-project src/Web
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

- **CQRS via MediatR**: Commands mutate state; queries read from MongoDB projections.
- **Ardalis API Endpoints**: One class per route in `src/Web/Endpoints/Modules/{Module}/`.
- **Modular Service Registration**: Each layer exposes an `IServiceInstaller` implementation; `Program.cs` discovers them via Scrutor assembly scanning (`InstallServicesFromAssemblies()`).
- **Module Contracts**: The `*.Shared` project in each module holds commands/queries/responses used across module boundaries.
- **Unit of Work + Repository**: Persistence abstractions wrap EF Core; transactions commit via `IUnitOfWork`.
- **MassTransit**: Abstracts the event bus (RabbitMQ transport in all environments). Consumer registration follows the MassTransit convention scanning pattern.

### Databases

| Store | Purpose | Default connection target |
|---|---|---|
| PostgreSQL | Transactional data, Hangfire jobs | `societiza-postgres-dev:5432` |
| MongoDB | Read projections (per module) | `societiza-mongodb-dev:27017` |
| RabbitMQ | Event bus transport | `societiza-rabbitmq-dev:5672` |

### Adding a New Endpoint

1. Create `{Operation}Endpoint.cs` in `src/Web/Endpoints/Modules/{Module}/`.
2. Create the MediatR command/query + handler in the `{Module}.Application` layer.
3. Add validators in `{Module}.Application` — MediatR pipeline automatically runs `ValidationBehavior`.
4. If a new read model is needed, add projection handler in `{Module}.Persistence`.

### API Base Path

All routes are prefixed with `/api` (configured via `PathBase` middleware). API versioning uses URL segments.

### Logging & Observability

Serilog is configured for structured logging. `CorrelationId` middleware injects a correlation ID into all requests. `RequestTimestampMiddleware` stamps request timing.
