# Tarefa 8: WorkflowTemplate MVP — Application

> Parte 2 de 2 da implementação do WorkflowTemplate MVP.
> Cobre a camada de Application: handlers com validação de estado e existência, e validators com regra condicional de Options.
> Gerado em 02/04/2026.

---

## Visão Geral

Este documento implementa todas as alterações da camada Application do módulo WorkflowTemplate. Depende da Task 7 (Domain) — os erros `WorkflowTemplateErrors` e os behavior methods `void` do aggregate devem estar implementados antes de executar este documento.

**Regra arquitetural central:** o aggregate não valida nada — toda validação de estado e existência de Step/Task/Field pertence ao Handler. O padrão é uniforme: carregar aggregate → validar → chamar behavior method → persistir.

---

## Estrutura de Arquivos

```
WorkflowTemplate.Application/
└── UseCases/
    ├── Commands/
    │   ├── ActivateWorkflowTemplateHandler.cs   ← validação de transição de status
    │   ├── ArchiveWorkflowTemplateHandler.cs    ← validação de transição de status
    │   ├── UpdateStepHandler.cs                 ← guard StepNotFound
    │   ├── RemoveStepHandler.cs                 ← guard StepNotFound
    │   ├── AddTaskHandler.cs                    ← guard StepNotFound
    │   ├── UpdateTaskHandler.cs                 ← guards StepNotFound + TaskNotFound
    │   ├── RemoveTaskHandler.cs                 ← guards StepNotFound + TaskNotFound
    │   ├── AddStepFieldHandler.cs               ← guard StepNotFound
    │   ├── UpdateStepFieldHandler.cs            ← guards StepNotFound + StepFieldNotFound
    │   ├── AddTaskFieldHandler.cs               ← guards StepNotFound + TaskNotFound
    │   └── UpdateTaskFieldHandler.cs            ← guards StepNotFound + TaskNotFound + TaskFieldNotFound
    └── Validators/
        ├── AddTemplateStepFieldValidator.cs     ← regra condicional Options × Select
        ├── UpdateTemplateStepFieldValidator.cs  ← regra condicional Options × Select
        ├── AddTemplateStepTaskFieldValidator.cs ← regra condicional Options × Select
        └── UpdateTemplateStepTaskFieldValidator.cs ← regra condicional Options × Select
```

---

## Parte 1 — Handlers

### Padrão uniforme

Todos os handlers seguem a mesma sequência:
1. `LoadAggregateAsync` → retornar `NotFoundError` se falhar
2. Validar estado / existência no handler via `WorkflowTemplateErrors`
3. Chamar behavior method `void` no aggregate
4. `AppendEventsAsync`
5. `Result.Success()`

---

### ActivateWorkflowTemplateHandler.cs

```csharp
var templateResult = await applicationService.LoadAggregateAsync<WorkflowTemplate>(
    request.TemplateId, cancellationToken);

if (!templateResult.IsSuccess)
    return Result.Failure(new NotFoundError(new(
        "WorkflowTemplate.NotFound", "Template not found")));

var template = templateResult.Value;

if (template.Status != TemplateStatus.Draft)
    return Result.Failure(WorkflowTemplateErrors.InvalidStatusTransition(
        from: template.Status.Name, to: TemplateStatus.Active.Name));

if (!template.Steps.Any(s => s.Tasks.Count > 0))
    return Result.Failure(WorkflowTemplateErrors.ActivationRequiresStepWithTask);

template.Activate();

await applicationService.AppendEventsAsync(template, cancellationToken);
return Result.Success();
```

### ArchiveWorkflowTemplateHandler.cs

```csharp
var templateResult = await applicationService.LoadAggregateAsync<WorkflowTemplate>(
    request.TemplateId, cancellationToken);

if (!templateResult.IsSuccess)
    return Result.Failure(new NotFoundError(new(
        "WorkflowTemplate.NotFound", "Template not found")));

var template = templateResult.Value;

if (template.Status == TemplateStatus.Archived)
    return Result.Failure(WorkflowTemplateErrors.InvalidStatusTransition(
        from: template.Status.Name, to: TemplateStatus.Archived.Name));

template.Archive();

await applicationService.AppendEventsAsync(template, cancellationToken);
return Result.Success();
```

### UpdateStepHandler.cs / RemoveStepHandler.cs

Guard idêntico nos dois:

```csharp
if (!template.Steps.Any(s => s.Id == request.StepId))
    return Result.Failure(WorkflowTemplateErrors.StepNotFound(request.StepId));

template.UpdateStep(/* ou RemoveStep */);
```

### AddTaskHandler.cs

```csharp
if (!template.Steps.Any(s => s.Id == request.StepId))
    return Result.Failure(WorkflowTemplateErrors.StepNotFound(request.StepId));

template.AddTask(request.StepId, request.Name, request.Description, request.Order);
```

### UpdateTaskHandler.cs / RemoveTaskHandler.cs

Guards em dois níveis:

```csharp
var step = template.Steps.FirstOrDefault(s => s.Id == request.StepId);
if (step is null)
    return Result.Failure(WorkflowTemplateErrors.StepNotFound(request.StepId));

if (!step.Tasks.Any(t => t.Id == request.TaskId))
    return Result.Failure(WorkflowTemplateErrors.TaskNotFound(request.TaskId));

template.UpdateTask(/* ou RemoveTask */);
```

### AddStepFieldHandler.cs

```csharp
if (!template.Steps.Any(s => s.Id == request.StepId))
    return Result.Failure(WorkflowTemplateErrors.StepNotFound(request.StepId));

template.AddStepField(request.StepId, /* demais params */);
```

### UpdateStepFieldHandler.cs

```csharp
var step = template.Steps.FirstOrDefault(s => s.Id == request.StepId);
if (step is null)
    return Result.Failure(WorkflowTemplateErrors.StepNotFound(request.StepId));

if (!step.Fields.Any(f => f.Id == request.FieldId))
    return Result.Failure(WorkflowTemplateErrors.StepFieldNotFound(request.FieldId));

template.UpdateStepField(request.StepId, request.FieldId, /* demais params */);
```

### AddTaskFieldHandler.cs

```csharp
var step = template.Steps.FirstOrDefault(s => s.Id == request.StepId);
if (step is null)
    return Result.Failure(WorkflowTemplateErrors.StepNotFound(request.StepId));

if (!step.Tasks.Any(t => t.Id == request.TaskId))
    return Result.Failure(WorkflowTemplateErrors.TaskNotFound(request.TaskId));

template.AddTaskField(request.StepId, request.TaskId, /* demais params */);
```

### UpdateTaskFieldHandler.cs

Guards em três níveis:

```csharp
var step = template.Steps.FirstOrDefault(s => s.Id == request.StepId);
if (step is null)
    return Result.Failure(WorkflowTemplateErrors.StepNotFound(request.StepId));

var task = step.Tasks.FirstOrDefault(t => t.Id == request.TaskId);
if (task is null)
    return Result.Failure(WorkflowTemplateErrors.TaskNotFound(request.TaskId));

if (!task.Fields.Any(f => f.Id == request.FieldId))
    return Result.Failure(WorkflowTemplateErrors.TaskFieldNotFound(request.FieldId));

template.UpdateTaskField(request.StepId, request.TaskId, request.FieldId, /* demais params */);
```

---

## Parte 2 — Validators

**Regra:** aplicar nos 4 validators indicados. A comparação usa `.Name` — nunca comparar SmartEnum diretamente, sempre via string.

```csharp
// AddTemplateStepFieldValidator.cs
// UpdateTemplateStepFieldValidator.cs
// AddTemplateStepTaskFieldValidator.cs
// UpdateTemplateStepTaskFieldValidator.cs

RuleFor(x => x.Options)
    .NotEmpty()
    .When(x => x.FieldType == FieldType.Select.Name)
    .WithMessage("Options are required for Select fields.");

RuleFor(x => x.Options)
    .Null()
    .When(x => x.FieldType != FieldType.Select.Name)
    .WithMessage("Options must be null for non-Select fields.");
```

---

## Resumo das Alterações

| Arquivo | Tipo | O que muda |
|---|---|---|
| `ActivateWorkflowTemplateHandler.cs` | Alteração | Validação `Draft → Active` e `ActivationRequiresStepWithTask` |
| `ArchiveWorkflowTemplateHandler.cs` | Alteração | Validação `Archived → Archived` bloqueada |
| `UpdateStepHandler.cs` | Alteração | Guard `StepNotFound` |
| `RemoveStepHandler.cs` | Alteração | Guard `StepNotFound` |
| `AddTaskHandler.cs` | Alteração | Guard `StepNotFound` |
| `UpdateTaskHandler.cs` | Alteração | Guards `StepNotFound` + `TaskNotFound` |
| `RemoveTaskHandler.cs` | Alteração | Guards `StepNotFound` + `TaskNotFound` |
| `AddStepFieldHandler.cs` | Alteração | Guard `StepNotFound` |
| `UpdateStepFieldHandler.cs` | Alteração | Guards `StepNotFound` + `StepFieldNotFound` |
| `AddTaskFieldHandler.cs` | Alteração | Guards `StepNotFound` + `TaskNotFound` |
| `UpdateTaskFieldHandler.cs` | Alteração | Guards `StepNotFound` + `TaskNotFound` + `TaskFieldNotFound` |
| `*FieldValidator.cs` (4 arquivos) | Alteração | Regra condicional `Options` × `Select` |

---

*Gerado em 02/04/2026.*
