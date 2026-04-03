# Tarefa 9: WorkflowTemplate — Descrições Swagger

> Documentação dos endpoints do módulo WorkflowTemplate para integração com o frontend.
> Cobre as descrições de todos os endpoints com regras de negócio, valores aceitos e restrições.
> Gerado em 02/04/2026.

---

## Visão Geral

Este documento atualiza o campo `Description` do `[SwaggerOperation]` em todos os endpoints do módulo WorkflowTemplate. O objetivo é que o frontend consiga integrar sem ambiguidade — valores aceitos, regras de preenchimento e restrições de estado descritos diretamente no Swagger UI.

## Skills do Codex

Antes de executar esta task:

- Use `$societiza-architecture` em `codex-skills/societiza-architecture`
- Leia `docs/societiza-context.md` e `docs/domain-model.md`
- Não use `.claude/skills/architecture/SKILL.md` para esta task

---

## Estrutura de Arquivos

```
Web/Endpoints/Modules/WorkflowTemplate/
├── ActivateWorkflowTemplateEndpoint.cs
├── ArchiveWorkflowTemplateEndpoint.cs
├── AddStepEndpoint.cs
├── UpdateStepEndpoint.cs
├── RemoveStepEndpoint.cs
├── AddTaskEndpoint.cs
├── UpdateTaskEndpoint.cs
├── RemoveTaskEndpoint.cs
├── AddStepFieldEndpoint.cs
├── UpdateStepFieldEndpoint.cs
├── RemoveStepFieldEndpoint.cs
├── AddTaskFieldEndpoint.cs
├── UpdateTaskFieldEndpoint.cs
└── RemoveTaskFieldEndpoint.cs
```

---

## 1. Template

### ActivateWorkflowTemplateEndpoint.cs

```csharp
[SwaggerOperation(
    Summary = "Activate template",
    Description = """
        Activates a workflow template.

        **Allowed from:** `Draft`
        **Not allowed from:** `Active`, `Archived`

        The template must have at least one step containing at least one task before it can be activated.
        """,
    Tags = [Tags.WorkflowTemplate])]
```

### ArchiveWorkflowTemplateEndpoint.cs

```csharp
[SwaggerOperation(
    Summary = "Archive template",
    Description = """
        Archives a workflow template.

        **Allowed from:** `Draft`, `Active`
        **Not allowed from:** `Archived`
        """,
    Tags = [Tags.WorkflowTemplate])]
```

---

## 2. Steps

### AddStepEndpoint.cs

```csharp
[SwaggerOperation(
    Summary = "Add step",
    Description = """
        Adds a step to a workflow template.

        If the requested `order` is already taken, existing steps are shifted forward automatically.
        """,
    Tags = [Tags.WorkflowTemplate])]
```

### UpdateStepEndpoint.cs

```csharp
[SwaggerOperation(
    Summary = "Update step",
    Description = """
        Updates a step in a workflow template.

        If the requested `order` is already taken, existing steps are shifted forward automatically.
        """,
    Tags = [Tags.WorkflowTemplate])]
```

### RemoveStepEndpoint.cs

```csharp
[SwaggerOperation(
    Summary = "Remove step",
    Description = "Removes a step and all its tasks and fields from the template.",
    Tags = [Tags.WorkflowTemplate])]
```

---

## 3. Tasks

### AddTaskEndpoint.cs

```csharp
[SwaggerOperation(
    Summary = "Add task",
    Description = """
        Adds a task to a step.

        **taskType** accepted values:
        - `Manual` — task completed manually by a user
        - `Automatic` — task executed automatically by the system
        - `Approval` — task that requires explicit approval

        If the requested `order` is already taken, existing tasks are shifted forward automatically.
        """,
    Tags = [Tags.WorkflowTemplate])]
```

### UpdateTaskEndpoint.cs

```csharp
[SwaggerOperation(
    Summary = "Update task",
    Description = """
        Updates a task in a step.

        **taskType** accepted values:
        - `Manual` — task completed manually by a user
        - `Automatic` — task executed automatically by the system
        - `Approval` — task that requires explicit approval

        If the requested `order` is already taken, existing tasks are shifted forward automatically.
        """,
    Tags = [Tags.WorkflowTemplate])]
```

### RemoveTaskEndpoint.cs

```csharp
[SwaggerOperation(
    Summary = "Remove task",
    Description = "Removes a task and all its fields from the step.",
    Tags = [Tags.WorkflowTemplate])]
```

---

## 4. Step Fields

### AddStepFieldEndpoint.cs

```csharp
[SwaggerOperation(
    Summary = "Add field to step",
    Description = """
        Adds a field to a step.

        **fieldType** accepted values:
        - `Text` — single-line text input
        - `Boolean` — true/false toggle
        - `Integer` — numeric input
        - `Select` — dropdown with predefined options
        - `Date` — date picker

        `options` is required when `fieldType` is `Select`.
        `options` must be null for all other field types.
        """,
    Tags = [Tags.WorkflowTemplate])]
```

### UpdateStepFieldEndpoint.cs

```csharp
[SwaggerOperation(
    Summary = "Update field in step",
    Description = """
        Updates a field in a step.

        **fieldType** accepted values:
        - `Text` — single-line text input
        - `Boolean` — true/false toggle
        - `Integer` — numeric input
        - `Select` — dropdown with predefined options
        - `Date` — date picker

        `options` is required when `fieldType` is `Select`.
        `options` must be null for all other field types.
        """,
    Tags = [Tags.WorkflowTemplate])]
```

### RemoveStepFieldEndpoint.cs

```csharp
[SwaggerOperation(
    Summary = "Remove field from step",
    Description = "Removes a field from a step.",
    Tags = [Tags.WorkflowTemplate])]
```

---

## 5. Task Fields

### AddTaskFieldEndpoint.cs

```csharp
[SwaggerOperation(
    Summary = "Add field to task",
    Description = """
        Adds a field to a task.

        **fieldType** accepted values:
        - `Text` — single-line text input
        - `Boolean` — true/false toggle
        - `Integer` — numeric input
        - `Select` — dropdown with predefined options
        - `Date` — date picker

        `options` is required when `fieldType` is `Select`.
        `options` must be null for all other field types.
        """,
    Tags = [Tags.WorkflowTemplate])]
```

### UpdateTaskFieldEndpoint.cs

```csharp
[SwaggerOperation(
    Summary = "Update field in task",
    Description = """
        Updates a field in a task.

        **fieldType** accepted values:
        - `Text` — single-line text input
        - `Boolean` — true/false toggle
        - `Integer` — numeric input
        - `Select` — dropdown with predefined options
        - `Date` — date picker

        `options` is required when `fieldType` is `Select`.
        `options` must be null for all other field types.
        """,
    Tags = [Tags.WorkflowTemplate])]
```

### RemoveTaskFieldEndpoint.cs

```csharp
[SwaggerOperation(
    Summary = "Remove field from task",
    Description = "Removes a field from a task.",
    Tags = [Tags.WorkflowTemplate])]
```

---

*Gerado em 02/04/2026.*
