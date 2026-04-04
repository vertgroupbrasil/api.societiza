# Tarefa 10: Core e WorkflowTemplate — `StoreEvent.cs` e `Order` em Fields

> Cobre duas alterações independentes: correção do Event Store e adição de `Order` em fields.
> Deve ser aplicada antes de qualquer merge de branches sobre o módulo WorkflowTemplate.
> Gerado em 03/04/2026.

---

## Visão Geral

Este documento implementa duas alterações que devem entrar antes do restante do Rework:

1. **`StoreEvent.cs`** — As Tasks 7 e 8 introduziram `ResolveStepOrder` e `ResolveTaskOrder`, helpers que podem emitir múltiplos eventos em uma única chamada. Com `aggregate.Version`, todos os eventos do loop recebem a mesma versão, quebrando a sequência no event store, a reidratação do aggregate e as projeções MongoDB. O impacto nos outros módulos é neutro.

2. **`Order` em fields** — As Tasks adicionaram `Order` em steps e tasks mas não em fields. `TemplateStepField` e `TemplateStepTaskField` precisam do campo para que o frontend consiga ordenar fields antes de renderizar.

## Skills do Codex

Antes de executar esta task:

- Use `$societiza-architecture` em `codex-skills/societiza-architecture`
- Leia `docs/societiza-context.md` e `docs/domain-model.md`
- Não use `.claude/skills/architecture/SKILL.md` para esta task

---

## Estrutura de Arquivos

```
Core/
└── EventStore/
    └── StoreEvent.cs                             ← @event.Version

WorkflowTemplate.Domain/
├── Entities/
│   ├── TemplateStepField.cs                      ← propriedade Order
│   └── TemplateStepTaskField.cs                  ← propriedade Order
├── DomainEvents/
│   └── WorkflowTemplateDomainEvents.cs           ← Order em 4 events de field
└── Aggregates/
    └── WorkflowTemplate.cs                       ← Order nos 4 behavior methods de field

WorkflowTemplate.Application/
└── UseCases/Commands/
    ├── AddStepFieldHandler.cs                    ← Order no command → behavior method
    ├── UpdateStepFieldHandler.cs                 ← Order no command → behavior method
    ├── AddTaskFieldHandler.cs                    ← Order no command → behavior method
    └── UpdateTaskFieldHandler.cs                 ← Order no command → behavior method

WorkflowTemplate.Shared/
└── Commands/
    ├── AddStepFieldCommand.cs                    ← propriedade Order
    ├── UpdateStepFieldCommand.cs                 ← propriedade Order
    ├── AddTaskFieldCommand.cs                    ← propriedade Order
    └── UpdateTaskFieldCommand.cs                 ← propriedade Order

WorkflowTemplate.Persistence/
└── Projections/
    ├── ProjectionEventHandler.cs                 ← Order nos 4 handlers de field
    └── ProjectionModels/
        ├── TemplateStepField.cs                  ← propriedade Order
        └── TemplateStepTaskField.cs              ← propriedade Order
```

---

## Etapa 1 — Corrigir `StoreEvent.cs`

**Localização:** `Core/EventStore/StoreEvent.cs`

```csharp
// Antes
Version = aggregate.Version,

// Depois
Version = @event.Version,
```

---

## Etapa 2 — `Order` em Fields

### 2.1 Entidades

**Localização:** `WorkflowTemplate.Domain/Entities/TemplateStepField.cs`

```csharp
public int Order { get; internal set; }
```

**Localização:** `WorkflowTemplate.Domain/Entities/TemplateStepTaskField.cs`

```csharp
public int Order { get; internal set; }
```

### 2.2 Domain Events

**Localização:** `WorkflowTemplate.Domain/DomainEvents/WorkflowTemplateDomainEvents.cs`

Adicionar `Order` nos quatro records de field:

```csharp
public sealed record WorkflowTemplateStepFieldAdded(
    Guid TemplateId,
    Guid StepId,
    Guid FieldId,
    string Label,
    string FieldType,
    string? Options,
    int Order,
    DateTimeOffset CreatedAt,
    ulong Version) : Message, IDomainEvent;

public sealed record WorkflowTemplateStepFieldUpdated(
    Guid TemplateId,
    Guid StepId,
    Guid FieldId,
    string Label,
    string FieldType,
    string? Options,
    int Order,
    DateTimeOffset UpdatedAt,
    ulong Version) : Message, IDomainEvent;

public sealed record WorkflowTemplateStepTaskFieldAdded(
    Guid TemplateId,
    Guid StepId,
    Guid TaskId,
    Guid FieldId,
    string Label,
    string FieldType,
    string? Options,
    int Order,
    DateTimeOffset CreatedAt,
    ulong Version) : Message, IDomainEvent;

public sealed record WorkflowTemplateStepTaskFieldUpdated(
    Guid TemplateId,
    Guid StepId,
    Guid TaskId,
    Guid FieldId,
    string Label,
    string FieldType,
    string? Options,
    int Order,
    DateTimeOffset UpdatedAt,
    ulong Version) : Message, IDomainEvent;
```

### 2.3 Behavior Methods no Aggregate

**Localização:** `WorkflowTemplate.Domain/Aggregates/WorkflowTemplate.cs`

Atualizar os quatro behavior methods para receber e propagar `order`:

```csharp
public void AddStepField(Guid stepId, string label, string fieldType, string? options, int order)
{
    RaiseEvent<DomainEvents.WorkflowTemplateStepFieldAdded>(version => new(
        TemplateId: Id,
        StepId: stepId,
        FieldId: Guid.NewGuid(),
        Label: label,
        FieldType: fieldType,
        Options: options,
        Order: order,
        CreatedAt: DateTimeOffset.UtcNow,
        Version: version));
}

public void UpdateStepField(Guid stepId, Guid fieldId, string label, string fieldType, string? options, int order)
{
    RaiseEvent<DomainEvents.WorkflowTemplateStepFieldUpdated>(version => new(
        TemplateId: Id,
        StepId: stepId,
        FieldId: fieldId,
        Label: label,
        FieldType: fieldType,
        Options: options,
        Order: order,
        UpdatedAt: DateTimeOffset.UtcNow,
        Version: version));
}

public void AddTaskField(Guid stepId, Guid taskId, string label, string fieldType, string? options, int order)
{
    RaiseEvent<DomainEvents.WorkflowTemplateStepTaskFieldAdded>(version => new(
        TemplateId: Id,
        StepId: stepId,
        TaskId: taskId,
        FieldId: Guid.NewGuid(),
        Label: label,
        FieldType: fieldType,
        Options: options,
        Order: order,
        CreatedAt: DateTimeOffset.UtcNow,
        Version: version));
}

public void UpdateTaskField(Guid stepId, Guid taskId, Guid fieldId, string label, string fieldType, string? options, int order)
{
    RaiseEvent<DomainEvents.WorkflowTemplateStepTaskFieldUpdated>(version => new(
        TemplateId: Id,
        StepId: stepId,
        TaskId: taskId,
        FieldId: fieldId,
        Label: label,
        FieldType: fieldType,
        Options: options,
        Order: order,
        UpdatedAt: DateTimeOffset.UtcNow,
        Version: version));
}
```

### 2.4 Commands

**Localização:** `WorkflowTemplate.Shared/Commands/`

Adicionar `Order` nos quatro commands de field:

```csharp
// AddStepFieldCommand.cs / UpdateStepFieldCommand.cs
int Order

// AddTaskFieldCommand.cs / UpdateTaskFieldCommand.cs
int Order
```

### 2.5 Handlers

**Localização:** `WorkflowTemplate.Application/UseCases/Commands/`

Nos quatro handlers de field, passar `request.Order` ao behavior method correspondente.

### 2.6 Projection Models

**Localização:** `WorkflowTemplate.Persistence/Projections/ProjectionModels/`

```csharp
public record TemplateStepField(
    Guid Id,
    Guid TemplateId,
    Guid StepId,
    string Label,
    string FieldType,
    string? Options,
    int Order,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt) : IProjectionModel;

public record TemplateStepTaskField(
    Guid Id,
    Guid TemplateId,
    Guid StepId,
    Guid TaskId,
    string Label,
    string FieldType,
    string? Options,
    int Order,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt) : IProjectionModel;
```

### 2.7 ProjectionEventHandler

**Localização:** `WorkflowTemplate.Persistence/Projections/ProjectionEventHandler.cs`

Nos quatro handlers de field, incluir `Order: @event.Order` ao construir o projection model.

---

## Resumo das Alterações

| Arquivo | Tipo | O que muda |
|---|---|---|
| `Core/EventStore/StoreEvent.cs` | Correção | `@event.Version` em vez de `aggregate.Version` |
| `TemplateStepField.cs` (Entity) | Alteração | Propriedade `Order` |
| `TemplateStepTaskField.cs` (Entity) | Alteração | Propriedade `Order` |
| `WorkflowTemplateDomainEvents.cs` | Alteração | `Order` em 4 records de field |
| `WorkflowTemplate.cs` (Aggregate) | Alteração | `Order` nos 4 behavior methods de field |
| `AddStepFieldCommand.cs` | Alteração | Propriedade `Order` |
| `UpdateStepFieldCommand.cs` | Alteração | Propriedade `Order` |
| `AddTaskFieldCommand.cs` | Alteração | Propriedade `Order` |
| `UpdateTaskFieldCommand.cs` | Alteração | Propriedade `Order` |
| `AddStepFieldHandler.cs` | Alteração | Passa `Order` ao behavior method |
| `UpdateStepFieldHandler.cs` | Alteração | Passa `Order` ao behavior method |
| `AddTaskFieldHandler.cs` | Alteração | Passa `Order` ao behavior method |
| `UpdateTaskFieldHandler.cs` | Alteração | Passa `Order` ao behavior method |
| `ProjectionModel.TemplateStepField` | Alteração | Propriedade `Order` |
| `ProjectionModel.TemplateStepTaskField` | Alteração | Propriedade `Order` |
| `ProjectionEventHandler.cs` | Alteração | `Order` nos 4 handlers de field |

---

*Gerado em 03/04/2026.*
