---
name: infrastructure
description: >
  Use this skill when running EF Core migrations (dotnet ef migrations add, dotnet ef database
  update), managing the local Docker Compose environment (PostgreSQL, MongoDB, RabbitMQ),
  building or running the production Docker image, reasoning about environment variables,
  connection strings, or the CI/CD pipeline. Contains exact commands for every DbContext
  (IdentityDbContext, AccountancyDbContext, WorkflowTemplateDbContext), the environment
  variable loading pitfall that breaks dotnet ef, Docker Compose service names and ports,
  Dockerfile multi-stage build decisions, and the GitHub Actions pipeline structure. Read
  this skill before touching any migration, .env file, docker-compose file, or Dockerfile.
---

# Infrastructure

## How to Use This Skill

Read the section that matches your task. Never guess commands or connection strings — this file
has the exact values.

- **EF Core / Migrations** — adding or applying migrations for any module
- **Docker Compose** — managing the local infrastructure (PostgreSQL, MongoDB, RabbitMQ)
- **Dockerfile** — understanding the production image or preparing for deploy
- **CI/CD** — understanding the GitHub Actions pipeline

---

## EF Core / Migrations

### DbContext per Module

Each module has its own isolated EF Core `DbContext` in `{Module}.Persistence`. There is no
shared context.

```
Identity         → IdentityDbContext         → src/Modules/Identity/Identity.Persistence
Accountancy      → AccountancyDbContext      → src/Modules/Accountancy/Accountancy.Persistence
WorkflowTemplate → WorkflowTemplateDbContext → src/Modules/WorkflowTemplate/WorkflowTemplate.Persistence
```

Each module also has a `{Module}ProjectionDbContext` inheriting `MongoDbContext` — this one does
**not use EF Migrations** and is managed by MongoDB directly.

### Command Pattern

Every `dotnet ef` command requires three flags:

- `--project` → the module's `.Persistence` project (contains the DbContext and Migrations)
- `--startup-project` → always `src/Web` (where connection options are configured)
- `--context` → explicit DbContext name (required because `Web` references multiple contexts)

### Environment Variable Pitfall

`Program.cs` loads `.env` (not `.env.local`) via `DotNetEnv`:

```csharp
if (File.Exists(".env"))
{
    Env.Load();
}
```

`dotnet ef` invokes the startup project through the design-time host, so it runs this code.
However:

- `.env.local` is **not loaded automatically** — only `.env`
- If you only have `.env.local`, EF will not find the connection strings and will fail

**Solution** — export the required variable before running `dotnet ef`:

```bash
# PowerShell
$env:ConnectionStrings__Default = "Server=localhost;Port=5432;Username=societiza;Password=societiza;Database=societiza;"

# Alternatively: copy .env.local to .env at the repo root before running
# Do not commit .env — it is already in .gitignore
```

### Migration Commands

#### Identity

```bash
# Add migration
dotnet ef migrations add <MigrationName> \
  --project src/Modules/Identity/Identity.Persistence \
  --startup-project src/Web \
  --context IdentityDbContext

# Apply to database
dotnet ef database update \
  --project src/Modules/Identity/Identity.Persistence \
  --startup-project src/Web \
  --context IdentityDbContext
```

#### Accountancy

```bash
# Add migration
dotnet ef migrations add <MigrationName> \
  --project src/Modules/Accountancy/Accountancy.Persistence \
  --startup-project src/Web \
  --context AccountancyDbContext

# Apply to database
dotnet ef database update \
  --project src/Modules/Accountancy/Accountancy.Persistence \
  --startup-project src/Web \
  --context AccountancyDbContext
```

#### WorkflowTemplate

```bash
# Add migration
dotnet ef migrations add <MigrationName> \
  --project src/Modules/WorkflowTemplate/WorkflowTemplate.Persistence \
  --startup-project src/Web \
  --context WorkflowTemplateDbContext

# Apply to database
dotnet ef database update \
  --project src/Modules/WorkflowTemplate/WorkflowTemplate.Persistence \
  --startup-project src/Web \
  --context WorkflowTemplateDbContext
```

### Migrations Location

```
src/Modules/Identity/Identity.Persistence/Migrations/
src/Modules/Accountancy/Accountancy.Persistence/Migrations/
src/Modules/WorkflowTemplate/WorkflowTemplate.Persistence/Migrations/
```

### What to NEVER do — EF Core

- Never omit `--context` — `Web` references multiple DbContexts and EF will be unable to resolve the right one.
- Never run `dotnet ef` without first ensuring the connection string is available in the environment — it will fail silently or with a misleading error.
- Never run `dotnet ef` using `{Module}.Persistence` as the startup project — configuration and DI are only wired in `src/Web`.
- Never create or apply migrations for `{Module}ProjectionDbContext` — MongoDB contexts are schema-free and do not use EF Migrations.
- Never add a new module DbContext without registering it in `MigrateDatabaseStartupTask` — migrations will not run on startup.

---

## Docker Compose

### File

```
Docker/docker-compose.Infrastructure.Development.yaml
```

Starts only infrastructure dependencies — no API. The API runs locally via `dotnet run` or as a
separate container.

### Services

```
PostgreSQL → societiza-postgres-dev → localhost:5432
           → user: societiza / pass: societiza / db: societiza

MongoDB    → societiza-mongodb-dev  → localhost:27017
           → user: societiza / pass: societiza

RabbitMQ   → societiza-rabbitmq-dev → localhost:5672 (AMQP) + localhost:15672 (Management UI)
           → user: guest / pass: guest
```

- **Docker network**: `societiza-dev` — all containers share this network
- **Volumes**: `pg_data`, `mongodb_data`, `rabbitmq_data` (locally persistent)
- **Memory limit**: 512MB per service
- **RabbitMQ hostname**: `societiza-rmq` (used internally by the broker)

### Commands

```bash
# Start all infrastructure (detached)
docker-compose -f Docker/docker-compose.Infrastructure.Development.yaml up -d

# Stop without removing volumes
docker-compose -f Docker/docker-compose.Infrastructure.Development.yaml down

# Stop and wipe volumes (full local state reset)
docker-compose -f Docker/docker-compose.Infrastructure.Development.yaml down -v

# Check service status
docker-compose -f Docker/docker-compose.Infrastructure.Development.yaml ps

# Stream logs (all services)
docker-compose -f Docker/docker-compose.Infrastructure.Development.yaml logs -f

# Stream logs (single service)
docker-compose -f Docker/docker-compose.Infrastructure.Development.yaml logs -f postgres

# Restart a single service
docker-compose -f Docker/docker-compose.Infrastructure.Development.yaml restart rabbitmq
```

### Connection Strings When Running the API via `dotnet run`

`.env.local` uses container names as hosts — those resolve only inside the `societiza-dev` Docker
network. When running the API with `dotnet run` outside Docker, use `localhost`:

```
ConnectionStrings__Default=Server=localhost;Port=5432;Username=societiza;Password=societiza;Database=societiza;
Projections__Identity=mongodb://societiza:societiza@localhost:27017/societiza?authSource=admin
EventBusOptions__ConnectionString=amqp://guest:guest@localhost:5672/
```

### What to NEVER do — Docker Compose

- Never use container hostnames (`societiza-postgres-dev`, `societiza-mongodb-dev`, `societiza-rabbitmq-dev`) in connection strings when running the API with `dotnet run` — they only resolve inside the Docker network.
- Never run `down -v` unless you intend to wipe all local data — volumes are persistent by design.

---

## Dockerfile

**File**: `Dockerfile` at the repository root.

Uses a **multi-stage build** with two stages. Base images are `mcr.microsoft.com/dotnet/sdk:8.0-alpine`
for the build stage and `mcr.microsoft.com/dotnet/aspnet:8.0-alpine` for the runtime stage.
Alpine was chosen to minimize image size.

### Stage 1: `build`

**Image**: `mcr.microsoft.com/dotnet/sdk:8.0-alpine`

**`.csproj` and `.sln` are copied first** — deliberate Docker layer cache decision. As long as
project files do not change, `dotnet restore` is skipped on rebuild.

**Source copy includes** Core + all modules (Accountancy, Identity, WorkflowTemplate) + Web.
The `tests/` directory is intentionally excluded — test projects must not be present in the
production image.

**`dotnet restore src/Web/Web.csproj`** restores from the entry point only. EF Core Design is
declared in `Web.csproj` with `PrivateAssets=all` so it does not leak into the runtime image.

**`dotnet publish src/Web/Web.csproj -c Release -o /app`** compiles in Release mode and publishes
all artifacts, including transitive module dependencies, to `/app`.

Output: `/app` with all published binaries.

### Stage 2: `runtime`

**Image**: `mcr.microsoft.com/dotnet/aspnet:8.0-alpine` — ASP.NET Core runtime only, no SDK.

**A non-root user is created** (`appuser` in group `appgroup`) — the process does not run as root
inside the container.

**`ASPNETCORE_URLS=http://+:8080`** — the app listens on port 8080. Ports 80 and 443 require root
and cannot be used with a non-root user. External port mapping (`-p 5258:8080`) is done at
`docker run`.

**`EXPOSE 8080`** documents the exposed port — metadata only, does not bind.

**Only `/app` is copied from the `build` stage** — no SDK, no source code, no `.csproj` files
reach the final image.

**`USER appuser`** switches to the non-root user before the entrypoint.

**`ENTRYPOINT ["dotnet", "Web.dll"]`** invokes the published assembly directly.

Output: lean production image with no SDK and no source.

### Runtime Environment Variables

The image embeds no secrets. All sensitive configuration must be injected via `--env-file` or
the orchestrator's secret management:

```
ASPNETCORE_ENVIRONMENT            → Development / Production
ConnectionStrings__Default        → PostgreSQL — transactional + Hangfire
ConnectionStrings__Hangfire       → PostgreSQL — Hangfire jobs
Projections__Identity             → MongoDB connection string
Projections__Accountancy          → MongoDB connection string
Projections__WorkflowTemplate     → MongoDB connection string
EventBusOptions__ConnectionString → RabbitMQ AMQP URI
EmailOptions__Username            → Brevo SMTP username
EmailOptions__Password            → Brevo SMTP key
EmailOptions__FromEmail           → Sender address
JwtOptions__*                     → JWT secret, issuer, audience
Cors__AllowedOrigins__0           → Allowed CORS origins
```

### Docker Commands

```bash
# Build image
docker build --no-cache -t societiza-api .

# Build with version tag
docker build --no-cache -t societiza-api:1.0.0 .

# Run container connected to dev network (infra must be up)
docker run --rm \
  -p 5258:8080 \
  --network societiza-dev \
  --env-file .env.local \
  societiza-api

# Run in detached mode
docker run -d \
  --name societiza-api \
  -p 5258:8080 \
  --network societiza-dev \
  --env-file .env.local \
  societiza-api

# Stream container logs
docker logs -f societiza-api

# Stop and remove
docker stop societiza-api && docker rm societiza-api
```

After `docker run`:
- API: `http://localhost:5258/api`
- Swagger: `http://localhost:5258/api/swagger` (only in `Development`)

### What to NEVER do — Dockerfile

- Never hardcode secrets or connection strings in the Dockerfile — inject via `--env-file` or orchestrator.
- Never copy `tests/` into the build stage — test projects must not be present in the production image.
- Never change `ASPNETCORE_URLS` to port 80 or 443 — those require root and would break the non-root user setup.
- Never promote the runtime stage from `aspnet` to `sdk` — the SDK must not be present in the production image.
- Never remove the `USER appuser` instruction — running as root is a security regression.
- Never remove `PrivateAssets=all` from EF Core Design in `Web.csproj` — without it, EF Design tooling leaks into the runtime image.

---

## CI/CD — GitHub Actions

**Pipeline**: `.github/workflows/societiza_bff_web.yml`

```
push on any branch
  └── build   (dotnet restore + dotnet build --configuration Release)
       └── test    (dotnet test --configuration Release)
            └── docker  (docker build --no-cache -t societiza-api .)
```

- **Build and test** run on `ubuntu-latest` with .NET 8.x
- **Docker step** validates that the image builds successfully — does not push to any registry
- Registry push for production is not yet implemented in CI