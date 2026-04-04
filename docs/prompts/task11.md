# Tarefa 11: WorkflowTemplate — Reordenação Automática de `Order` em Fields

> Complemento das Tasks 7 e 10.
> Adiciona `ResolveStepFieldOrder` e `ResolveTaskFieldOrder` ao aggregate, e `ShiftOrder()` nas entidades `TemplateStepField` e `TemplateStepTaskField`.
> Gerado em 04/04/2026.

---

## Visão Geral

As Tasks 7 e 10 implementaram reordenação automática em `WorkflowTemplateStep` (via `ResolveStepOrder`) e em `WorkflowTemplateStepTask` (via `ResolveTaskOrder`), junto com os helpers `ShiftOrder()` nas entidades correspondentes.

O mesmo mecanismo estava ausente em `TemplateStepField` e `TemplateStepTaskField`. Sem ele, adicionar ou atualizar um field com um `Order` já ocupado causa colisão silenciosa — dois fields ficam com a mesma posição e a ordenação retornada ao frontend se torna não-determinística.

Esta task implementa a lacuna seguindo o padrão já estabelecido:

1. `ShiftOrder()` interno em `TemplateStepField` e `TemplateStepTaskField`
2. `ResolveStepFieldOrder(stepId, requestedOrder, excludeFieldId?)` no aggregate
3. `ResolveTaskFieldOrder(stepId, taskId, requestedOrder, excludeFieldId?)` no aggregate
4. Uso dos resolvers em `AddStepField`, `UpdateStepField`, `AddTaskField` e `UpdateTaskField`

Nenhuma outra camada é alterada. Os domain events, commands, handlers, validators, projection models e projection handlers já carregam e persistem `Order` (implementado na Task 10) — eles continuam inalterados.

## Skills

Antes de executar esta task:

- Use `.claude/skills/architecture/SKILL.md`
- Leia `docs/societiza-context.md` e `docs/domain-model.md`

**Regras arquiteturais aplicadas:**
- Aggregate não retorna `Result` nem lança exceptions — behavior methods são sempre `void`
- Estado mutado exclusivamente via `RaiseEvent<T>()` — nunca atribuição direta fora de `When()`
- `ShiftOrder()` é mutação direta de propriedade local da entidade (sem evento), exatamente como já implementado em `WorkflowTemplateStep` e `WorkflowTemplateStepTask`
- `Version` nunca hardcoded — sempre via delegate do `RaiseEvent<T>()`

---

## Estrutura de Arquivos

Apenas dois arquivos são alterados:

```
WorkflowTemplate.Domain/
├── Entities/
│   ├── TemplateStepField.cs          ← adicionar ShiftOrder()
│   └── TemplateStepTaskField.cs      ← adicionar ShiftOrder()
└── Aggregates/
    └── WorkflowTemplate.cs           ← ResolveStepFieldOrder(), ResolveTaskFieldOrder(),
                                          e uso nos 4 behavior methods de field
```

---

## 1. TemplateStepField.cs

**Localização:** `WorkflowTemplate.Domain/Entities/TemplateStepField.cs`

Adicionar o método `ShiftOrder` na classe, seguindo o mesmo padrão de `WorkflowTemplateStep.ShiftOrder` e `WorkflowTemplateStepTask.ShiftOrder`:

```csharp
internal void ShiftOrder(int newOrder) => Order = newOrder;
```

O modificador `internal` garante que apenas o aggregate (no mesmo assembly de domínio) possa invocar a mutação direta de ordem.

---

## 2. TemplateStepTaskField.cs

**Localização:** `WorkflowTemplate.Domain/Entities/TemplateStepTaskField.cs`

Mesma adição:

```csharp
internal void ShiftOrder(int newOrder) => Order = newOrder;
```

---

## 3. WorkflowTemplate.cs (Aggregate) — Helpers privados

**Localização:** `WorkflowTemplate.Domain/Aggregates/WorkflowTemplate.cs`

Adicionar os dois helpers privados de resolução de ordem de field, imediatamente após `ResolveTaskOrder` (o último helper existente).

### ResolveStepFieldOrder

Resolve colisão de `Order` entre fields de uma mesma etapa. Exclui o field sendo atualizado (`excludeFieldId`) para que `UpdateStepField` não conflite com o próprio field alvo.

```csharp
private int ResolveStepFieldOrder(Guid stepId, int requestedOrder, Guid? excludeFieldId = null)
{
    var step = Steps.FirstOrDefault(s => s.Id == stepId)!; // Handler garantiu existência
    var siblings = step.Fields
        .Where(f => excludeFieldId is null || f.Id != excludeFieldId)
        .OrderBy(f => f.Order)
        .ToList();

    if (siblings.All(f => f.Order != requestedOrder))
        return requestedOrder;

    foreach (var sibling in siblings.Where(f => f.Order >= requestedOrder))
        sibling.ShiftOrder(sibling.Order + 1);

    return requestedOrder;
}
```

### ResolveTaskFieldOrder

Resolve colisão de `Order` entre fields de uma mesma tarefa. Navega dois níveis: `Steps → Tasks → Fields`.

```csharp
private int ResolveTaskFieldOrder(Guid stepId, Guid taskId, int requestedOrder, Guid? excludeFieldId = null)
{
    var step = Steps.FirstOrDefault(s => s.Id == stepId)!;       // Handler garantiu existência
    var task = step.Tasks.FirstOrDefault(t => t.Id == taskId)!;  // Handler garantiu existência
    var siblings = task.Fields
        .Where(f => excludeFieldId is null || f.Id != excludeFieldId)
        .OrderBy(f => f.Order)
        .ToList();

    if (siblings.All(f => f.Order != requestedOrder))
        return requestedOrder;

    foreach (var sibling in siblings.Where(f => f.Order >= requestedOrder))
        sibling.ShiftOrder(sibling.Order + 1);

    return requestedOrder;
}
```

> **Nota sobre o `!`:** o `FirstOrDefault()!` é seguro nos dois helpers pelo mesmo motivo que em `ResolveTaskOrder`: o handler já validou a existência do step e da task antes de chamar o behavior method. O aggregate não precisa defender esse caminho.

---

## 4. WorkflowTemplate.cs (Aggregate) — Behavior Methods

**Localização:** `WorkflowTemplate.Domain/Aggregates/WorkflowTemplate.cs`

Atualizar os quatro behavior methods de field para chamar os resolvers antes de emitir o evento. A assinatura dos métodos e os parâmetros dos domain events não mudam — apenas o valor de `Order` passado ao evento passa a ser o `resolvedOrder` em vez de `order` diretamente.

### AddStepField

```csharp
public void AddStepField(Guid stepId, string label, string fieldType, string? options, int order)
{
    var resolvedOrder = ResolveStepFieldOrder(stepId, order);

    RaiseEvent<DomainEvents.WorkflowTemplateStepFieldAdded>(version => new(
        TemplateId: Id,
        StepId: stepId,
        FieldId: Guid.NewGuid(),
        Label: label,
        FieldType: fieldType,
        Options: options,
        Order: resolvedOrder,
        CreatedAt: DateTimeOffset.UtcNow,
        Version: version));
}
```

### UpdateStepField

```csharp
public void UpdateStepField(Guid stepId, Guid fieldId, string label, string fieldType, string? options, int order)
{
    var resolvedOrder = ResolveStepFieldOrder(stepId, order, excludeFieldId: fieldId);

    RaiseEvent<DomainEvents.WorkflowTemplateStepFieldUpdated>(version => new(
        TemplateId: Id,
        StepId: stepId,
        FieldId: fieldId,
        Label: label,
        FieldType: fieldType,
        Options: options,
        Order: resolvedOrder,
        UpdatedAt: DateTimeOffset.UtcNow,
        Version: version));
}
```

### AddTaskField

```csharp
public void AddTaskField(Guid stepId, Guid taskId, string label, string fieldType, string? options, int order)
{
    var resolvedOrder = ResolveTaskFieldOrder(stepId, taskId, order);

    RaiseEvent<DomainEvents.WorkflowTemplateStepTaskFieldAdded>(version => new(
        TemplateId: Id,
        StepId: stepId,
        TaskId: taskId,
        FieldId: Guid.NewGuid(),
        Label: label,
        FieldType: fieldType,
        Options: options,
        Order: resolvedOrder,
        CreatedAt: DateTimeOffset.UtcNow,
        Version: version));
}
```

### UpdateTaskField

```csharp
public void UpdateTaskField(Guid stepId, Guid taskId, Guid fieldId, string label, string fieldType, string? options, int order)
{
    var resolvedOrder = ResolveTaskFieldOrder(stepId, taskId, order, excludeFieldId: fieldId);

    RaiseEvent<DomainEvents.WorkflowTemplateStepTaskFieldUpdated>(version => new(
        TemplateId: Id,
        StepId: stepId,
        TaskId: taskId,
        FieldId: fieldId,
        Label: label,
        FieldType: fieldType,
        Options: options,
        Order: resolvedOrder,
        UpdatedAt: DateTimeOffset.UtcNow,
        Version: version));
}
```

---

## Resumo das Alterações

| Arquivo | Tipo | O que muda |
|---|---|---|
| `TemplateStepField.cs` (Entity) | Alteração | Adiciona `ShiftOrder(int newOrder)` |
| `TemplateStepTaskField.cs` (Entity) | Alteração | Adiciona `ShiftOrder(int newOrder)` |
| `WorkflowTemplate.cs` (Aggregate) | Alteração | Adiciona `ResolveStepFieldOrder(...)` e `ResolveTaskFieldOrder(...)` |
| `WorkflowTemplate.cs` (Aggregate) | Alteração | `AddStepField` usa `ResolveStepFieldOrder` |
| `WorkflowTemplate.cs` (Aggregate) | Alteração | `UpdateStepField` usa `ResolveStepFieldOrder` com `excludeFieldId` |
| `WorkflowTemplate.cs` (Aggregate) | Alteração | `AddTaskField` usa `ResolveTaskFieldOrder` |
| `WorkflowTemplate.cs` (Aggregate) | Alteração | `UpdateTaskField` usa `ResolveTaskFieldOrder` com `excludeFieldId` |

Nenhum outro arquivo é alterado. Domain events, commands, handlers, validators, projection models e projection handlers permanecem inalterados.

---

## O que os testes devem verificar

Nenhum arquivo de teste novo é obrigatório, mas os testes existentes de domínio devem ser estendidos para cobrir os novos casos. Se o projeto tiver `WorkflowTemplateFieldBehaviorTests`, adicionar cenários ali. Caso contrário, adicionar em `WorkflowTemplateAggregateTests`.

Casos mínimos a cobrir:

- `AddStepField` com `Order` livre → persiste o `Order` solicitado sem alteração
- `AddStepField` com `Order` ocupado → desloca field existente e persiste o `Order` solicitado para o novo field
- `UpdateStepField` com `Order` livre → persiste o `Order` solicitado sem alteração
- `UpdateStepField` com o próprio `Order` (sem mudança de posição) → não desloca nenhum sibling
- `UpdateStepField` com `Order` ocupado por outro field → desloca o outro field e persiste o `Order` solicitado
- Mesmos quatro cenários repetidos para `AddTaskField` e `UpdateTaskField`

## Regras

Siga o padrão de código do projeto, verifique a reordenação automática em `WorkflowTemplateStep` (via `ResolveStepOrder`) e em `WorkflowTemplateStepTask` (via `ResolveTaskOrder`), junto com os helpers `ShiftOrder()` nas entidades correspondentes para entender o padrão de código do projeto (identação, escrita, etc.).

---

*Gerado em 04/04/2026.*