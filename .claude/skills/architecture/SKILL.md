---
name: architecture
description: >
  Use this skill for any code change in the Societiza repository — features, bug fixes,
  refactors, migrations, or cleanups. Contains the mandatory patterns for every layer:
  Domain (Aggregate, SmartEnum, Domain Events, Projection Models), Shared (Commands, Email
  Commands, Response DTOs), Application (Handlers, Validators, Projection Handlers, Job
  Handlers), Persistence (Event Store, EF Core configurations, MongoDB projections),
  Infrastructure (ServiceInstallers, Scrutor scan, ModuleInstaller), and Web (Endpoints,
  Routes, MassTransit Consumers). Also covers naming conventions, what NOT to do per layer,
  and the feature implementation checklist. If you are editing any .cs or .csproj file,
  read this skill first.
---

# Architecture Base

## Module Structure

```
src/
├── Core/                        # Cross-cutting infrastructure (shared)
├── Modules/
│   ├── {Module}.Domain/
│   ├── {Module}.Application/
│   ├── {Module}.Infrastructure/
│   ├── {Module}.Persistence/
│   └── {Module}.Shared/
└── Web/                         # ASP.NET Core host — entry point
    └── Endpoints/Modules/{Module}/
```

Existing modules: `Accountancy` · `Identity` · `WorkflowTemplate`

---

## How to Use This Skill

Before implementing anything, read the reference file for the layer you are about to work on. Never rely on generic .NET conventions — always follow the patterns documented here.

- `references/domain-architecture.md` — Aggregate, Domain Events, SmartEnum, Projection Models
- `references/shared-architecture.md` — Commands, Email Commands, Response DTOs
- `references/application-architecture.md` — Handler, Validator, Projection Handler, Email Handler, Job Handler
- `references/persistence-architecture.md` — Event Store, MongoDB Projections, EF Configurations
- `references/infrastructure-architecture.md` — ModuleInstaller, ServiceInstallers, Scrutor scan, ApplicationService
- `references/web-architecture.md` — Endpoint, Request DTO, Routes, railway-oriented pipeline, MassTransit Consumers

Start with `references/domain-architecture.md` if you are modeling a new feature.
Start with `references/checklist.md` if you are implementing an existing model end-to-end.