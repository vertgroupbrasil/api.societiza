# Tarefa 7: WorkflowTemplate MVP — Domain

> Parte 1 de 2 da implementação do WorkflowTemplate MVP.
> Cobre exclusivamente a camada de Domain: erros, entidades, helpers de reordenação e behavior methods do aggregate.
> Gerado em 02/04/2026.

---

## Visão Geral

Este documento implementa todas as alterações da camada Domain do módulo WorkflowTemplate. A camada Application (handlers e validators) é coberta na Task 8, que depende deste documento.

## Skills do Codex

Antes de executar esta task:

- Use `$societiza-architecture` em `codex-skills/societiza-architecture`
- Leia `docs/societiza-context.md` e `docs/domain-model.md`
- Não use `.claude/skills/architecture/SKILL.md` para esta task

**Regras arquiteturais aplicadas:**
- Aggregate não retorna `Result` nem lança exceptions — behavior methods são sempre `void`
- Estado mutado exclusivamente via `RaiseEvent<T>()` — nunca atribuição direta fora de `When()`
- `Version` nunca hardcoded — sempre via delegate do `RaiseEvent<T>()`

---

## Estrutura de Arquivos

```
WorkflowTemplate.Domain/
├── Errors/
│   └── WorkflowTemplateErrors.cs          ← adicionar 6 novos erros
├── Entities/
│   ├── WorkflowTemplateStep.cs            ← adicionar ShiftOrder()
│   └── WorkflowTemplateStepTask.cs        ← adicionar ShiftOrder()
└── Aggregates/
    └── WorkflowTemplate.cs                ← Activate(), Archive(), helpers, todos os behavior methods
```

---

## 1. WorkflowTemplateErrors.cs

**Localização:** `WorkflowTemplate.Domain/Errors/WorkflowTemplateErrors.cs`

Adicionar junto aos erros existentes:

```csharp
public static Error InvalidStatusTransition(string from, string to) =>
    Error.Validation("WorkflowTemplate.InvalidStatusTransition",
        $"Cannot transition from '{from}' to '{to}'.");

public static readonly Error ActivationRequiresStepWithTask =
    Error.Validation("WorkflowTemplate.ActivationRequiresStepWithTask",
        "Template must have at least one step with at least one task to be activated.");

public static Error StepNotFound(Guid stepId) =>
    Error.NotFound("WorkflowTemplate.StepNotFound",
        $"Step '{stepId}' was not found.");

public static Error TaskNotFound(Guid taskId) =>
    Error.NotFound("WorkflowTemplate.TaskNotFound",
        $"Task '{taskId}' was not found.");

public static Error StepFieldNotFound(Guid fieldId) =>
    Error.NotFound("WorkflowTemplate.StepFieldNotFound",
        $"Step field '{fieldId}' was not found.");

public static Error TaskFieldNotFound(Guid fieldId) =>
    Error.NotFound("WorkflowTemplate.TaskFieldNotFound",
        $"Task field '{fieldId}' was not found.");
```

---

## 2. WorkflowTemplateStep.cs

**Localização:** `WorkflowTemplate.Domain/Entities/WorkflowTemplateStep.cs`

Adicionar na classe:

```csharp
internal void ShiftOrder(int newOrder) => Order = newOrder;
```

---

## 3. WorkflowTemplateStepTask.cs

**Localização:** `WorkflowTemplate.Domain/Entities/WorkflowTemplateStepTask.cs`

Adicionar na classe:

```csharp
internal void ShiftOrder(int newOrder) => Order = newOrder;
```

---

## 4. WorkflowTemplate.cs (Aggregate)

**Localização:** `WorkflowTemplate.Domain/Aggregates/WorkflowTemplate.cs`

### Helpers privados de reordenação

Adicionar ao aggregate. O `FirstOrDefault()!` em `ResolveTaskOrder` é seguro — o Handler valida existência antes de chamar qualquer behavior method.

```csharp
private int ResolveStepOrder(int requestedOrder, Guid? excludeStepId = null)
{
    var siblings = Steps
        .Where(s => excludeStepId is null || s.Id != excludeStepId)
        .OrderBy(s => s.Order)
        .ToList();

    if (siblings.All(s => s.Order != requestedOrder))
        return requestedOrder;

    foreach (var sibling in siblings.Where(s => s.Order >= requestedOrder))
        sibling.ShiftOrder(sibling.Order + 1);

    return requestedOrder;
}

private int ResolveTaskOrder(Guid stepId, int requestedOrder, Guid? excludeTaskId = null)
{
    var step = Steps.FirstOrDefault(s => s.Id == stepId)!; // Handler garantiu existência
    var siblings = step.Tasks
        .Where(t => excludeTaskId is null || t.Id != excludeTaskId)
        .OrderBy(t => t.Order)
        .ToList();

    if (siblings.All(t => t.Order != requestedOrder))
        return requestedOrder;

    foreach (var sibling in siblings.Where(t => t.Order >= requestedOrder))
        sibling.ShiftOrder(sibling.Order + 1);

    return requestedOrder;
}
```

### Substituir Activate() (linha ~47)

```csharp
public void Activate()
{
    RaiseEvent<DomainEvents.WorkflowTemplateActivated>(version => new(
        Id: Id,
        UpdatedAt: DateTimeOffset.UtcNow,
        Version: version));
}
```

### Substituir Archive() (linha ~51)

```csharp
public void Archive()
{
    RaiseEvent<DomainEvents.WorkflowTemplateArchived>(version => new(
        Id: Id,
        UpdatedAt: DateTimeOffset.UtcNow,
        Version: version));
}
```

### Substituir AddStep() (linha ~55)

```csharp
public void AddStep(string name, string? description, int order)
{
    var resolvedOrder = ResolveStepOrder(order);

    RaiseEvent<DomainEvents.WorkflowTemplateStepAdded>(version => new(
        Id: Id,
        StepId: Guid.NewGuid(),
        Name: name,
        Description: description,
        Order: resolvedOrder,
        UpdatedAt: DateTimeOffset.UtcNow,
        Version: version));
}
```

### Substituir UpdateStep() (linha ~63)

```csharp
public void UpdateStep(Guid stepId, string name, string? description, int order)
{
    var resolvedOrder = ResolveStepOrder(order, excludeStepId: stepId);

    RaiseEvent<DomainEvents.WorkflowTemplateStepUpdated>(version => new(
        Id: Id,
        StepId: stepId,
        Name: name,
        Description: description,
        Order: resolvedOrder,
        UpdatedAt: DateTimeOffset.UtcNow,
        Version: version));
}
```

### Substituir RemoveStep() (linha ~73)

```csharp
public void RemoveStep(Guid stepId)
{
    RaiseEvent<DomainEvents.WorkflowTemplateStepRemoved>(version => new(
        Id: Id,
        StepId: stepId,
        UpdatedAt: DateTimeOffset.UtcNow,
        Version: version));
}
```

### Substituir AddTask() (linha ~80)

```csharp
public void AddTask(Guid stepId, string name, string? description, int order)
{
    var resolvedOrder = ResolveTaskOrder(stepId, order);

    RaiseEvent<DomainEvents.WorkflowTemplateStepTaskAdded>(version => new(
        Id: Id,
        StepId: stepId,
        TaskId: Guid.NewGuid(),
        Name: name,
        Description: description,
        Order: resolvedOrder,
        UpdatedAt: DateTimeOffset.UtcNow,
        Version: version));
}
```

### Substituir UpdateTask() (linha ~91)

```csharp
public void UpdateTask(Guid stepId, Guid taskId, string name, string? description, int order)
{
    var resolvedOrder = ResolveTaskOrder(stepId, order, excludeTaskId: taskId);

    RaiseEvent<DomainEvents.WorkflowTemplateStepTaskUpdated>(version => new(
        Id: Id,
        StepId: stepId,
        TaskId: taskId,
        Name: name,
        Description: description,
        Order: resolvedOrder,
        UpdatedAt: DateTimeOffset.UtcNow,
        Version: version));
}
```

### Substituir RemoveTask() (linha ~103)

```csharp
public void RemoveTask(Guid stepId, Guid taskId)
{
    RaiseEvent<DomainEvents.WorkflowTemplateStepTaskRemoved>(version => new(
        Id: Id,
        StepId: stepId,
        TaskId: taskId,
        UpdatedAt: DateTimeOffset.UtcNow,
        Version: version));
}
```

### Substituir AddStepField() (linha ~111)

Remover o `throw new ArgumentException(...)` existente. Substituir pela versão sem guard:

```csharp
public void AddStepField(Guid stepId, /* demais params */)
{
    RaiseEvent<DomainEvents.WorkflowTemplateStepFieldAdded>(version => new(
        // ... params
        Version: version));
}
```

### Substituir AddTaskField() (linha ~150)

Remover o `throw new ArgumentException(...)` existente. Substituir pela versão sem guard:

```csharp
public void AddTaskField(Guid stepId, Guid taskId, /* demais params */)
{
    RaiseEvent<DomainEvents.WorkflowTemplateStepTaskFieldAdded>(version => new(
        // ... params
        Version: version));
}
```

---

*Gerado em 02/04/2026.*
