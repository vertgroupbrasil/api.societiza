# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Context Documents

Always import the following documents at the start of every conversation. They are the canonical references for the project and domain:

- @docs/societiza-context.md — Product context, business rules, glossary, and tech stack overview.
- @docs/domain-model.md — Canonical domain model: aggregates, entities, value objects, domain events, projection models and enumerations.

---

## Skills

Project skills are located in `.claude/skills/`. Read the relevant skill before starting any task:

- @.claude/skills/architecture/SKILL.md — required before touching any `.cs` or `.csproj`.
- @.claude/skills/infrastructure/SKILL.md — required before running migrations or Docker commands.

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

For detailed architecture patterns, layer conventions, and the feature implementation checklist, see `.claude/skills/architecture/SKILL.md`.

### Databases

- **PostgreSQL** — transactional data + Hangfire jobs (`societiza-postgres-dev:5432`)
- **MongoDB** — read projections per module (`societiza-mongodb-dev:27017`)
- **RabbitMQ** — event bus transport (`societiza-rabbitmq-dev:5672`)

## Email

- **Provider**: Brevo via SMTP relay — no SDK or external package. Chosen for its generous free tier (300 emails/day) and no verified domain requirement initially.
- **Integration**: native `System.Net.Mail.SmtpClient` only; do not add external email packages.
- **Development**: use Brevo SMTP with a personal email as both sender and recipient for testing.
- **Production**: Brevo SMTP relay at `smtp-relay.brevo.com`, port `587`, SSL enabled.
- **Future**: when `societiza.com.br` is available, update `FromEmail` to `noreply@societiza.com.br`.
- **Security**: the Brevo SMTP Key must **never** be committed. Store it in `.env.local` (already in `.gitignore`).

### Usage pattern

`IEmailService` is a generic delivery abstraction with a single method `SendAsync(toEmail, toName, subject, body, ct)`. It **has no knowledge of domain concepts** like "invite" or "welcome" — that is each module's responsibility.

**Never inject `IEmailService` directly into business handlers.** Sending must always be asynchronous via Hangfire:

1. Each module creates its own email commands in `{Module}.Shared/Emails/` (command record) and `{Module}.Application/UseCases/Emails/` (handler)
2. Email command handlers inject `IEmailService`, build the subject and HTML body, and call `SendAsync`
3. Business handlers inject `IJobSchedulerService` and enqueue: `_jobScheduler.Enqueue(new SendXxxEmailCommand(...))`

```
// Per-module structure
{Module}.Shared/
└── Emails/
    └── Send{Action}EmailCommand.cs        // sealed record, primitives only (string, Guid)

{Module}.Application/
└── UseCases/Emails/
    └── Send{Action}EmailCommandHandler.cs // injects IEmailService, builds subject/body
```

The command lives in `Shared` so it can be referenced without coupling to the Application layer. Email commands must contain **only primitive types** in their parameters to ensure Hangfire serialization.