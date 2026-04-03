---
name: societiza-infrastructure
description: >
  Societiza infrastructure and operations guide for EF Core migrations, Docker Compose,
  Docker image builds, local environment variables, connection strings, and CI/CD behavior.
  Use when editing Docker files, compose files, .env files, migration files, startup
  configuration, or when running dotnet ef, docker-compose, docker build, or related
  operational commands in this repository.
---

# Societiza Infrastructure

Use this skill before running migrations or changing operational configuration. Read the section
that matches the task and use the exact commands and connection values documented here.

## How to Use This Skill

Read the section that matches the task. Never guess commands or connection strings; use the exact
values documented here.

- **EF Core / Migrations**: adding or applying migrations for any module
- **Docker Compose**: managing PostgreSQL, MongoDB, and RabbitMQ locally
- **Dockerfile**: understanding or changing the production image
- **CI/CD**: understanding the GitHub Actions pipeline

## EF Core / Migrations

### DbContext per Module

Each module has its own isolated EF Core `DbContext` in `{Module}.Persistence`. There is no
shared context.

```text
Identity         -> IdentityDbContext         -> src/Modules/Identity/Identity.Persistence
Accountancy      -> AccountancyDbContext      -> src/Modules/Accountancy/Accountancy.Persistence
WorkflowTemplate -> WorkflowTemplateDbContext -> src/Modules/WorkflowTemplate/WorkflowTemplate.Persistence
```

Each module also has a `{Module}ProjectionDbContext` inheriting `MongoDbContext`. Projection
contexts do not use EF migrations and are managed by MongoDB directly.

### Command Pattern

Every `dotnet ef` command requires three flags:

- `--project`: the module's `.Persistence` project
- `--startup-project`: always `src/Web`
- `--context`: the explicit `DbContext` name

### Environment Variable Pitfall

`Program.cs` loads `.env`, not `.env.local`, through `DotNetEnv`:

```csharp
if (File.Exists(".env"))
{
    Env.Load();
}
```

When `dotnet ef` runs through the design-time host:

- `.env.local` is not loaded automatically
- missing connection strings will break the command

Set the required environment variable first:

```powershell
$env:ConnectionStrings__Default = "Server=localhost;Port=5432;Username=societiza;Password=societiza;Database=societiza;"
```

Alternatively, copy `.env.local` to `.env` at the repository root without committing `.env`.

### Migration Commands

#### Identity

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Modules/Identity/Identity.Persistence \
  --startup-project src/Web \
  --context IdentityDbContext

dotnet ef database update \
  --project src/Modules/Identity/Identity.Persistence \
  --startup-project src/Web \
  --context IdentityDbContext
```

#### Accountancy

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Modules/Accountancy/Accountancy.Persistence \
  --startup-project src/Web \
  --context AccountancyDbContext

dotnet ef database update \
  --project src/Modules/Accountancy/Accountancy.Persistence \
  --startup-project src/Web \
  --context AccountancyDbContext
```

#### WorkflowTemplate

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Modules/WorkflowTemplate/WorkflowTemplate.Persistence \
  --startup-project src/Web \
  --context WorkflowTemplateDbContext

dotnet ef database update \
  --project src/Modules/WorkflowTemplate/WorkflowTemplate.Persistence \
  --startup-project src/Web \
  --context WorkflowTemplateDbContext
```

### Migrations Location

```text
src/Modules/Identity/Identity.Persistence/Migrations/
src/Modules/Accountancy/Accountancy.Persistence/Migrations/
src/Modules/WorkflowTemplate/WorkflowTemplate.Persistence/Migrations/
```

### What to Never Do for EF Core

- Never omit `--context`.
- Never run `dotnet ef` before making the connection string available in the environment.
- Never use `{Module}.Persistence` as the startup project.
- Never create migrations for `{Module}ProjectionDbContext`.
- Never add a new module `DbContext` without registering it in `MigrateDatabaseStartupTask`.

## Docker Compose

### File

```text
Docker/docker-compose.Infrastructure.Development.yaml
```

This file starts only the infrastructure dependencies. Run the API separately with `dotnet run`
or as a standalone container.

### Services

```text
PostgreSQL -> societiza-postgres-dev -> localhost:5432
           -> user: societiza / pass: societiza / db: societiza

MongoDB    -> societiza-mongodb-dev  -> localhost:27017
           -> user: societiza / pass: societiza

RabbitMQ   -> societiza-rabbitmq-dev -> localhost:5672 (AMQP) + localhost:15672 (Management UI)
           -> user: guest / pass: guest
```

- Docker network: `societiza-dev`
- Volumes: `pg_data`, `mongodb_data`, `rabbitmq_data`
- Memory limit: `512MB` per service
- RabbitMQ hostname: `societiza-rmq`

### Commands

```bash
docker-compose -f Docker/docker-compose.Infrastructure.Development.yaml up -d
docker-compose -f Docker/docker-compose.Infrastructure.Development.yaml down
docker-compose -f Docker/docker-compose.Infrastructure.Development.yaml down -v
docker-compose -f Docker/docker-compose.Infrastructure.Development.yaml ps
docker-compose -f Docker/docker-compose.Infrastructure.Development.yaml logs -f
docker-compose -f Docker/docker-compose.Infrastructure.Development.yaml logs -f postgres
docker-compose -f Docker/docker-compose.Infrastructure.Development.yaml restart rabbitmq
```

### Connection Strings for `dotnet run`

When the API runs outside Docker, use `localhost` instead of container hostnames:

```text
ConnectionStrings__Default=Server=localhost;Port=5432;Username=societiza;Password=societiza;Database=societiza;
Projections__Identity=mongodb://societiza:societiza@localhost:27017/societiza?authSource=admin
EventBusOptions__ConnectionString=amqp://guest:guest@localhost:5672/
```

### What to Never Do for Docker Compose

- Never use container hostnames in connection strings when running the API with `dotnet run`.
- Never run `down -v` unless you intend to wipe all local data.

## Dockerfile

**File**: `Dockerfile` at the repository root.

Use a multi-stage build with `mcr.microsoft.com/dotnet/sdk:8.0-alpine` for build and
`mcr.microsoft.com/dotnet/aspnet:8.0-alpine` for runtime.

### Build Stage

- Copy `.csproj` and `.sln` files first to preserve Docker layer cache.
- Restore from `src/Web/Web.csproj`.
- Exclude `tests/` from the production image.
- Publish with `dotnet publish src/Web/Web.csproj -c Release -o /app`.

### Runtime Stage

- Run on `mcr.microsoft.com/dotnet/aspnet:8.0-alpine`.
- Create and use a non-root user.
- Keep `ASPNETCORE_URLS=http://+:8080`.
- Expose port `8080`.
- Copy only `/app` from the build stage.
- Start with `ENTRYPOINT ["dotnet", "Web.dll"]`.

### Runtime Environment Variables

```text
ASPNETCORE_ENVIRONMENT
ConnectionStrings__Default
ConnectionStrings__Hangfire
Projections__Identity
Projections__Accountancy
Projections__WorkflowTemplate
EventBusOptions__ConnectionString
EmailOptions__Username
EmailOptions__Password
EmailOptions__FromEmail
JwtOptions__*
Cors__AllowedOrigins__0
```

### Docker Commands

```bash
docker build --no-cache -t societiza-api .
docker build --no-cache -t societiza-api:1.0.0 .
docker run --rm -p 5258:8080 --network societiza-dev --env-file .env.local societiza-api
docker run -d --name societiza-api -p 5258:8080 --network societiza-dev --env-file .env.local societiza-api
docker logs -f societiza-api
docker stop societiza-api && docker rm societiza-api
```

After `docker run`:

- API: `http://localhost:5258/api`
- Swagger: `http://localhost:5258/api/swagger` in `Development`

### What to Never Do for the Dockerfile

- Never hardcode secrets or connection strings in the Dockerfile.
- Never copy `tests/` into the build stage.
- Never change `ASPNETCORE_URLS` to port `80` or `443`.
- Never promote the runtime stage from `aspnet` to `sdk`.
- Never remove `USER appuser`.
- Never remove `PrivateAssets=all` from EF Core Design in `Web.csproj`.

## CI/CD

Pipeline file: `.github/workflows/societiza_bff_web.yml`

```text
push on any branch
  `-- build   (dotnet restore + dotnet build --configuration Release)
       `-- test    (dotnet test --configuration Release)
            `-- docker  (docker build --no-cache -t societiza-api .)
```

- Build and test run on `ubuntu-latest` with .NET 8.x
- Docker validates that the image builds successfully
- Registry push for production is not implemented yet
