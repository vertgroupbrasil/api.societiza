---
name: societiza-architecture
description: >
  Societiza repository architecture guide for backend code changes, including features, bug fixes,
  refactors, migrations, cleanups, and reviews across the Domain, Shared, Application,
  Persistence, Infrastructure, and Web layers. Use when editing any .cs or .csproj file,
  modeling a new feature, or validating that a change follows the project's DDD, CQRS,
  modular monolith, and event-driven conventions.
---

# Societiza Architecture

Use this skill as the default entry point for code changes in the Societiza backend. Read the
project context first, then load only the layer references that match the task.

## Quick Start

1. Read [societiza-context.md](../../docs/societiza-context.md) and
   [domain-model.md](../../docs/domain-model.md).
2. Identify which module and layers the task touches.
3. Read every matching file in `references/` before editing code.
4. Use `references/checklist.md` when implementing or reviewing an end-to-end feature.

## Module Structure

```text
src/
|-- Core/                        # Cross-cutting infrastructure shared by all modules
|-- Modules/
|   |-- {Module}.Domain/
|   |-- {Module}.Application/
|   |-- {Module}.Infrastructure/
|   |-- {Module}.Persistence/
|   `-- {Module}.Shared/
`-- Web/                         # ASP.NET Core host and entry point
    `-- Endpoints/Modules/{Module}/
```

Existing modules: `Accountancy`, `Identity`, `WorkflowTemplate`

## Reference Map

- `references/domain-architecture.md`: Read before changing aggregates, entities, value objects,
  SmartEnum types, domain events, or projection models.
- `references/shared-architecture.md`: Read before changing commands, email commands, shared DTOs,
  or other cross-layer contracts.
- `references/application-architecture.md`: Read before changing handlers, validators, projection
  handlers, email handlers, or job handlers.
- `references/persistence-architecture.md`: Read before changing event store behavior, EF Core
  configurations, migrations, or MongoDB projections.
- `references/infrastructure-architecture.md`: Read before changing installers, DI registrations,
  Scrutor scanning, or cross-cutting services.
- `references/web-architecture.md`: Read before changing endpoints, request DTOs, routing,
  MassTransit consumers, or the web pipeline.
- `references/checklist.md`: Read when implementing or reviewing a feature from end to end.

## Working Rules

- Treat the documented layer boundaries as mandatory.
- Prefer the existing Societiza patterns over generic .NET defaults.
- Start with `references/domain-architecture.md` when modeling a new feature.
- Read all relevant layer references when a change crosses module boundaries.
- Keep new code aligned with the project's DDD, CQRS, and event-driven modular monolith style.
