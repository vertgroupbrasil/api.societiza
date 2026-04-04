# Módulo WorkflowTemplate — Documentação Técnica Detalhada

Este documento descreve, em detalhes, o módulo `WorkflowTemplate` da API Societiza: sua arquitetura, responsabilidades, componentes e decisões de design.

O módulo é o bounded context responsável por definir e gerenciar modelos de fluxo de trabalho corporativo (abertura de empresa, alteração, encerramento). Ele atravessa todas as camadas da arquitetura:

- domínio (aggregate, entidades, eventos, enumerações, value objects, projeções);
- camada compartilhada (commands, queries, responses, errors);
- camada de aplicação (handlers, validators, event handlers);
- persistência (event store PostgreSQL + read model MongoDB);
- infraestrutura (instaladores, application service);
- camada web (endpoints HTTP, consumers MassTransit);
- testes automatizados.

---

## 1. Objetivo Geral do Módulo

### O que o módulo faz

O módulo `WorkflowTemplate` permite que firmas de contabilidade definam templates reutilizáveis de fluxo de trabalho. Um template descreve a sequência de etapas que um processo corporativo percorre — cada etapa contendo tarefas a executar e campos de dados a preencher.

O ciclo de vida de um template passa por três estados: `Draft` (em construção), `Active` (publicado e disponível para uso) e `Archived` (descontinuado, somente leitura).

A hierarquia de entidades dentro de um template é:

```
WorkflowTemplate (AggregateRoot)
└── WorkflowTemplateStep (Entity)  [1..*]
    ├── TemplateStepField (Entity)  [0..*]
    └── WorkflowTemplateStepTask (Entity)  [1..*]
        └── TemplateStepTaskField (Entity)  [0..*]
```

### Por que esse design

Toda a estrutura hierárquica é controlada por um único aggregate root (`WorkflowTemplate`). Isso garante que nenhuma entidade filha exista fora do contexto de um template e que todas as mutações passem pelo mesmo pipeline de eventos de domínio, mantendo consistência e rastreabilidade.

---

## 2. Aggregate Root — WorkflowTemplate

### Arquivos

- `src/Modules/WorkflowTemplate/WorkflowTemplate.Domain/Aggregates/WorkflowTemplate.cs`

### O que faz

O aggregate root é o coração do módulo. Ele centraliza todo o estado do template e expõe métodos de comportamento que emitem eventos de domínio a cada mutação.

**Propriedades:**
- `Name` (`string`) — nome do template
- `Description` (`string`) — descrição do propósito
- `TemplateVersion` (`TemplateVersion`) — versionamento semântico (value object)
- `Status` (`TemplateStatus`) — estado atual: Draft, Active ou Archived
- `Steps` (`List<WorkflowTemplateStep>`) — etapas ordenadas do fluxo

**Factory method:**
- `WorkflowTemplate.Create(name, description)` — instancia o aggregate em status `Draft` e emite `WorkflowTemplateCreated`

**Métodos de comportamento:**
- `Update(name, description)` — atualiza metadados e emite `WorkflowTemplateUpdated`
- `Activate()` — transição `Draft → Active`; valida que existe ao menos uma etapa com ao menos uma tarefa; emite `WorkflowTemplateActivated`
- `Archive()` — transição `Active → Archived`; emite `WorkflowTemplateArchived`
- `AddStep / UpdateStep / RemoveStep` — gerencia etapas; emite os eventos de step correspondentes
- `AddTask / UpdateTask / RemoveTask` — gerencia tarefas dentro de uma etapa
- `AddStepField / UpdateStepField / RemoveStepField` — gerencia campos de etapa
- `AddTaskField / UpdateTaskField / RemoveTaskField` — gerencia campos de tarefa

**Resolução de ordem:**
O aggregate implementa quatro helpers privados de resolução de `Order`, um para cada tipo de entidade ordenável:

- `ResolveStepOrder(requestedOrder, excludeStepId?)` — deslocamento de steps; emite `WorkflowTemplateStepUpdated` para cada sibling deslocado.
- `ResolveTaskOrder(stepId, requestedOrder, excludeTaskId?)` — deslocamento de tasks; emite `WorkflowTemplateStepTaskUpdated` para cada sibling deslocado.
- `ResolveStepFieldOrder(stepId, requestedOrder, excludeFieldId?)` — deslocamento de fields de etapa via `ShiftOrder()` direto na entidade, sem emissão de evento para siblings.
- `ResolveTaskFieldOrder(stepId, taskId, requestedOrder, excludeFieldId?)` — deslocamento de fields de tarefa, mesmo comportamento.

Quando um elemento é inserido ou atualizado em uma posição já ocupada, todos os siblings com `Order >= requestedOrder` são deslocados para frente. O parâmetro `exclude*Id` é usado nos updates para que o próprio elemento não conflite consigo mesmo.

**Reconstituição de estado:**
O aggregate usa `ApplyEvent(IDomainEvent)` com dispatch dinâmico para métodos `When(...)`. Há um handler `When` para cada um dos 16 eventos de domínio do módulo. Isso permite que o aggregate seja reconstituído a partir da sequência de eventos do event store sem lógica externa.

### Por que isso importa

Concentrar toda a mutação no aggregate garante que o domínio nunca seja corrompido por alterações parciais. Um step nunca pode ser adicionado sem que o aggregate emita um evento. Uma ativação nunca acontece sem que a regra de negócio seja verificada. Todo o histórico de mudanças fica registrado no event store.

---

## 3. Entidades do Aggregate

### Arquivos

- `src/Modules/WorkflowTemplate/WorkflowTemplate.Domain/Entities/WorkflowTemplateStep.cs`
- `src/Modules/WorkflowTemplate/WorkflowTemplate.Domain/Entities/WorkflowTemplateStepTask.cs`
- `src/Modules/WorkflowTemplate/WorkflowTemplate.Domain/Entities/TemplateStepField.cs`
- `src/Modules/WorkflowTemplate/WorkflowTemplate.Domain/Entities/TemplateStepTaskField.cs`

### O que faz

**`WorkflowTemplateStep`** representa uma etapa do fluxo (ex.: "Verificação de viabilidade", "Registro", "Alvará"). Cada step pertence a exatamente um template.

Propriedades: `TemplateId`, `Title`, `Description`, `Order`, `Fields` (lista de `TemplateStepField`), `Tasks` (lista de `WorkflowTemplateStepTask`).

Métodos: `Update(...)`, `ShiftOrder(newOrder)`, `AddTask / UpdateTask / RemoveTask`, `AddField / UpdateField / RemoveField`.

---

**`WorkflowTemplateStepTask`** representa uma tarefa a executar dentro de uma etapa (ex.: "Preencher formulário de viabilidade", "Enviar DARE"). Pode ser manual, automática ou de aprovação.

Propriedades: `Title`, `Description`, `Type` (`TaskType`), `Configuration` (JSON, padrão `{}`), `Order`, `IsOptional`, `Fields` (lista de `TemplateStepTaskField`).

O campo `Configuration` armazena parâmetros de automação serializados em JSON para tarefas do tipo `Automatic` — cada tipo de automação de portal municipal define seu próprio schema de configuração.

---

**`TemplateStepField`** define um campo de dado associado a uma etapa. Propriedades: `StepId`, `Label`, `FieldType`, `Options` (JSON para campos Select), `Order`. Método: `Update(...)`, `ShiftOrder(newOrder)` — mutação direta de `Order` usada pelo `ResolveStepFieldOrder` do aggregate.

**`TemplateStepTaskField`** define um campo de dado associado a uma tarefa específica. Mesmas propriedades que `TemplateStepField`, mas com `TaskId` no lugar de `StepId`. Método: `Update(...)`, `ShiftOrder(newOrder)` — mesmo padrão.

### Por que isso importa

As entidades são objetos filhos do aggregate — elas não têm existência independente e não são acessadas diretamente por repositórios externos. Toda operação sobre elas passa pelo aggregate root, que garante a integridade da hierarquia e emite os eventos corretos.

---

## 4. Value Object — TemplateVersion

### Arquivo

- `src/Modules/WorkflowTemplate/WorkflowTemplate.Domain/ValueObjects/TemplateVersion.cs`

### O que faz

`TemplateVersion` encapsula o versionamento semântico de um template com dois campos inteiros: `Major` e `Minor`.

É imutável — uma vez criado, não pode ser alterado diretamente. Qualquer mudança estrutural relevante no template deve gerar uma nova versão.

Métodos:
- `ToString()` — retorna `"v{Major}.{Minor}"` (ex.: `"v1.2"`)
- `Parse(string)` — desserializa a string `"vX.Y"` de volta para o value object

### Por que isso importa

Versionar um template de forma explícita permite rastrear evoluções do fluxo sem quebrar instâncias de processos em andamento. A versão é transportada nos eventos de domínio e armazenada no read model, tornando o histórico auditável.

---

## 5. Enumerações — TemplateStatus, TaskType, FieldType

### Arquivos

- `src/Modules/WorkflowTemplate/WorkflowTemplate.Domain/Enumerations/TemplateStatus.cs`
- `src/Modules/WorkflowTemplate/WorkflowTemplate.Domain/Enumerations/TaskType.cs`
- `src/Modules/WorkflowTemplate/WorkflowTemplate.Domain/Enumerations/FieldType.cs`

### O que fazem

Todas as enumerações do módulo são implementadas com `Ardalis.SmartEnum` e suportam conversão implícita para `string` (via `Name`) e para `int` (via `Value`).

**`TemplateStatus`:**
- `Draft` (0) — em construção, não disponível para uso
- `Active` (1) — publicado e disponível para criar processos
- `Archived` (2) — descontinuado, somente leitura

**`TaskType`:**
- `Manual` (0) — executada manualmente pelo operador
- `Automatic` (1) — executada pelo sistema (ex.: automação de portal municipal)
- `Approval` (2) — requer aprovação explícita antes de avançar

**`FieldType`:**
- `Text` (0) — texto livre
- `Boolean` (1) — verdadeiro / falso
- `Integer` (2) — número inteiro
- `Select` (3) — seleção entre opções pré-definidas (usa `Options` JSON)
- `Date` (4) — data

### Por que isso importa

SmartEnum evita magic strings e magic numbers no código. A conversão implícita garante que valores transportados como `string` nos eventos de domínio e nas projeções possam ser comparados e convertidos sem boilerplate, e que erros de digitação sejam capturados em tempo de compilação.

---

## 6. Eventos de Domínio

### Arquivo

- `src/Modules/WorkflowTemplate/WorkflowTemplate.Domain/Events/DomainEvents.cs`

### O que faz

O módulo possui 16 eventos de domínio. Todos são `sealed record` que implementam `Message` e `IDomainEvent`. Os valores de `Status` e `Type` são transportados como `string` nos eventos — a conversão para SmartEnum ocorre nos handlers `When(...)` do aggregate durante a reconstituição.

**Eventos de template (4):**
- `WorkflowTemplateCreated` — `Id, Name, Description, TemplateVersion, Status, CreatedAt, Version`
- `WorkflowTemplateUpdated` — `Id, Name, Description, UpdatedAt, Version`
- `WorkflowTemplateActivated` — `Id, UpdatedAt, Version`
- `WorkflowTemplateArchived` — `Id, UpdatedAt, Version`

**Eventos de step (3):**
- `WorkflowTemplateStepAdded` — `TemplateId, StepId, Title, Description, Order, CreatedAt, Version`
- `WorkflowTemplateStepUpdated` — `TemplateId, StepId, Title, Description, Order, UpdatedAt, Version`
- `WorkflowTemplateStepRemoved` — `TemplateId, StepId, FieldIds[], TaskIds[], TaskFieldIds[], Version`

> `WorkflowTemplateStepRemoved` carrega os IDs das entidades filhas (`FieldIds`, `TaskIds`, `TaskFieldIds`) para permitir deleção em cascata no read model sem queries adicionais.

**Eventos de task (3):**
- `WorkflowTemplateStepTaskAdded` — `TemplateId, StepId, TaskId, Title, Description, Type, Configuration, Order, IsOptional, CreatedAt, Version`
- `WorkflowTemplateStepTaskUpdated` — mesmos campos + `UpdatedAt`
- `WorkflowTemplateStepTaskRemoved` — `TemplateId, StepId, TaskId, TaskFieldIds[], Version`

**Eventos de step field (3):**
- `WorkflowTemplateStepFieldAdded` — `TemplateId, StepId, FieldId, Label, FieldType, Options, Order, CreatedAt, Version`
- `WorkflowTemplateStepFieldUpdated` — mesmos campos + `UpdatedAt`
- `WorkflowTemplateStepFieldRemoved` — `TemplateId, StepId, FieldId, Version`

**Eventos de task field (3):**
- `WorkflowTemplateStepTaskFieldAdded` — `TemplateId, StepId, TaskId, FieldId, Label, FieldType, Options, Order, CreatedAt, Version`
- `WorkflowTemplateStepTaskFieldUpdated` — mesmos campos + `UpdatedAt`
- `WorkflowTemplateStepTaskFieldRemoved` — `TemplateId, StepId, TaskId, FieldId, Version`

### Por que isso importa

Eventos são a fonte de verdade do write model. Cada mutação produz exatamente um evento — isso torna o histórico auditável, permite reconstituição do aggregate e alimenta o read model via MassTransit sem acoplamento direto entre as camadas.

---

## 7. Projection Models

### Arquivo

- `src/Modules/WorkflowTemplate/WorkflowTemplate.Domain/Projection/ProjectionModel.cs`

### O que faz

Os projection models são os read models armazenados no MongoDB. Todos são `record` que implementam `IProjectionModel`. Ao contrário do write model (que recria o aggregate a partir de eventos), o read model é desnormalizado e otimizado para consultas rápidas.

**`ProjectionModel.WorkflowTemplate`:**
`Id, Name, Description, TemplateVersion (string), Status (string), CreatedAt, UpdatedAt, IsDeleted`

**`ProjectionModel.WorkflowTemplateStep`:**
`Id, TemplateId, Title, Description, Order, CreatedAt, UpdatedAt`

**`ProjectionModel.WorkflowTemplateStepTask`:**
`Id, TemplateId, StepId, Title, Description, Type (string), Configuration, Order, IsOptional, CreatedAt, UpdatedAt`

**`ProjectionModel.TemplateStepField`:**
`Id, TemplateId, StepId, Label, FieldType (string), Options, Order, CreatedAt, UpdatedAt`

**`ProjectionModel.TemplateStepTaskField`:**
`Id, TemplateId, StepId, TaskId, Label, FieldType (string), Options, Order, CreatedAt, UpdatedAt`

### Por que isso importa

O read model é separado do write model por design (CQRS). Isso permite que as queries de leitura sejam ultra-rápidas (sem reconstituição de evento, sem joins relacionais) e que o schema de leitura evolua independentemente do schema de escrita. Cada entidade tem sua própria coleção no MongoDB, o que facilita queries por `TemplateId`, `StepId` ou `TaskId` sem precisar trazer o template inteiro.

---

## 8. Camada Shared — Commands

### Arquivos

```
src/Modules/WorkflowTemplate/WorkflowTemplate.Shared/Commands/
├── Template/
│   ├── CreateWorkflowTemplateCommand.cs
│   ├── UpdateWorkflowTemplateCommand.cs
│   ├── ActivateWorkflowTemplateCommand.cs
│   └── ArchiveWorkflowTemplateCommand.cs
├── TemplateStep/
│   ├── AddWorkflowTemplateStepCommand.cs
│   ├── UpdateWorkflowTemplateStepCommand.cs
│   └── RemoveWorkflowTemplateStepCommand.cs
├── TemplateStepTask/
│   ├── AddWorkflowTemplateStepTaskCommand.cs
│   ├── UpdateWorkflowTemplateStepTaskCommand.cs
│   └── RemoveWorkflowTemplateStepTaskCommand.cs
├── TemplateStepField/
│   ├── AddTemplateStepFieldCommand.cs
│   ├── UpdateTemplateStepFieldCommand.cs
│   └── RemoveTemplateStepFieldCommand.cs
└── TemplateStepTaskField/
    ├── AddTemplateStepTaskFieldCommand.cs
    ├── UpdateTemplateStepTaskFieldCommand.cs
    └── RemoveTemplateStepTaskFieldCommand.cs
```

### O que faz

São 16 `sealed record` que representam as intenções de mutação do sistema. Cada comando carrega apenas primitivos — sem objetos de domínio, sem dependências externas.

**Template:**
- `CreateWorkflowTemplateCommand(Name, Description)`
- `UpdateWorkflowTemplateCommand(Id, Name, Description)`
- `ActivateWorkflowTemplateCommand(Id)`
- `ArchiveWorkflowTemplateCommand(Id)`

**Step:**
- `AddWorkflowTemplateStepCommand(TemplateId, Title, Description, Order)`
- `UpdateWorkflowTemplateStepCommand(TemplateId, StepId, Title, Description, Order)`
- `RemoveWorkflowTemplateStepCommand(TemplateId, StepId)`

**Task:**
- `AddWorkflowTemplateStepTaskCommand(TemplateId, StepId, Title, Description, Type, Configuration, Order, IsOptional)`
- `UpdateWorkflowTemplateStepTaskCommand(TemplateId, StepId, TaskId, Title, Description, Type, Configuration, Order, IsOptional)`
- `RemoveWorkflowTemplateStepTaskCommand(TemplateId, StepId, TaskId)`

**Step Field:**
- `AddTemplateStepFieldCommand(TemplateId, StepId, Label, FieldType, Options, Order)`
- `UpdateTemplateStepFieldCommand(TemplateId, StepId, FieldId, Label, FieldType, Options, Order)`
- `RemoveTemplateStepFieldCommand(TemplateId, StepId, FieldId)`

**Task Field:**
- `AddTemplateStepTaskFieldCommand(TemplateId, StepId, TaskId, Label, FieldType, Options, Order)`
- `UpdateTemplateStepTaskFieldCommand(TemplateId, StepId, TaskId, FieldId, Label, FieldType, Options, Order)`
- `RemoveTemplateStepTaskFieldCommand(TemplateId, StepId, TaskId, FieldId)`

### Por que isso importa

Os commands ficam na camada `Shared` para que possam ser referenciados pelos endpoints da camada `Web` sem acoplar a `Web` à camada `Application`. Isso mantém a separação de responsabilidades entre quem declara a intenção (Shared) e quem a executa (Application).

---

## 9. Camada Shared — Queries e Respostas

### Arquivos

```
src/Modules/WorkflowTemplate/WorkflowTemplate.Shared/Queries/
├── GetAllWorkflowTemplatesQuery.cs
└── GetWorkflowTemplateDetailByIdQuery.cs

src/Modules/WorkflowTemplate/WorkflowTemplate.Shared/Response/
├── Template/
│   ├── WorkflowTemplateResponse.cs
│   └── WorkflowTemplateDetailResponse.cs
├── TemplateStep/
│   └── WorkflowTemplateStepFieldDetailResponse.cs
├── TemplateStepTask/
│   ├── WorkflowTemplateStepTaskDetailResponse.cs
│   └── WorkflowTemplateStepTaskFieldDetailResponse.cs
└── WorkflowTemplateStepDetailResponse.cs
```

### O que faz

**Queries:**
- `GetAllWorkflowTemplatesQuery()` — retorna `List<WorkflowTemplateResponse>` (visão de lista)
- `GetWorkflowTemplateDetailByIdQuery(Id)` — retorna `WorkflowTemplateDetailResponse` (visão detalhada com estrutura aninhada completa)

**Response DTOs:**

`WorkflowTemplateResponse` — visão de lista: `Id, Name, Description, Version, Status, CreatedAt`

`WorkflowTemplateDetailResponse` — visão detalhada: todos os campos acima + `UpdatedAt` + array aninhado de `Steps`

`WorkflowTemplateStepDetailResponse` — `Id, Title, Description, Order` + arrays de `Fields` e `Tasks`

`WorkflowTemplateStepFieldDetailResponse` — `Id, Label, FieldType, Options, Order`

`WorkflowTemplateStepTaskDetailResponse` — `Id, Title, Description, Type, Configuration, Order, IsOptional` + array de `Fields`

`WorkflowTemplateStepTaskFieldDetailResponse` — `Id, Label, FieldType, Options, Order`

### Por que isso importa

Os response DTOs são o contrato público da API. Separar visão de lista (campos mínimos) de visão detalhada (estrutura completa aninhada) evita over-fetching nas listas e garante que o detalhe tenha todos os dados necessários para renderizar o editor de template completo no frontend.

---

## 10. Erros de Domínio — WorkflowTemplateErrors

### Arquivo

- `src/Modules/WorkflowTemplate/WorkflowTemplate.Shared/Errors/WorkflowTemplateErrors.cs`

### O que faz

`WorkflowTemplateErrors` é uma classe estática com factory methods que produzem instâncias tipadas de erro. Cada método retorna um tipo de erro específico com código HTTP semântico.

| Método | Tipo de erro | HTTP |
|---|---|---|
| `StepTitleAlreadyExists(title)` | `ConflictError` | 409 |
| `TaskTitleAlreadyExists(title)` | `ConflictError` | 409 |
| `StepFieldLabelAlreadyExists(label)` | `ConflictError` | 409 |
| `TaskFieldLabelAlreadyExists(label)` | `ConflictError` | 409 |
| `InvalidStatusTransition(from, to)` | `Error` | — |
| `ActivationRequiresStepWithTask` | `Error` (estático) | — |
| `StepNotFound(stepId)` | `NotFoundError` | 404 |
| `TaskNotFound(taskId)` | `NotFoundError` | 404 |
| `StepFieldNotFound(fieldId)` | `NotFoundError` | 404 |
| `TaskFieldNotFound(fieldId)` | `NotFoundError` | 404 |

### Por que isso importa

Centralizar os erros em uma classe estática garante consistência nas mensagens e nos códigos HTTP retornados. Os handlers não produzem strings de erro ad hoc — eles chamam um factory method com semântica clara, facilitando manutenção e testes.

---

## 11. Application Handlers — Comandos

### Arquivos

```
src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Commands/
├── Template/
│   ├── CreateWorkflowTemplateHandler.cs
│   ├── UpdateWorkflowTemplateHandler.cs
│   ├── ActivateWorkflowTemplateHandler.cs
│   └── ArchiveWorkflowTemplateHandler.cs
├── TemplateStep/
│   ├── AddWorkflowTemplateStepHandler.cs
│   ├── UpdateWorkflowTemplateStepHandler.cs
│   └── RemoveWorkflowTemplateStepHandler.cs
├── TemplateStepTask/
│   ├── AddWorkflowTemplateStepTaskHandler.cs
│   ├── UpdateWorkflowTemplateStepTaskHandler.cs
│   └── RemoveWorkflowTemplateStepTaskHandler.cs
├── TemplateStepField/
│   ├── AddTemplateStepFieldHandler.cs
│   ├── UpdateTemplateStepFieldHandler.cs
│   └── RemoveTemplateStepFieldHandler.cs
└── TemplateStepTaskField/
    ├── AddTemplateStepTaskFieldHandler.cs
    ├── UpdateTemplateStepTaskFieldHandler.cs
    └── RemoveTemplateStepTaskFieldHandler.cs
```

### O que fazem

Todos os handlers de comando seguem o mesmo fluxo base: carregar o aggregate via `IWorkflowTemplateApplicationService.LoadAggregateAsync`, invocar o método de comportamento do aggregate, persistir os eventos via `AppendEventsAsync` e confirmar via `SaveChangesAsync`. Retornam `Result` ou `Result<IdentifierResponse>` pelo padrão railway-oriented.

**Handlers de template:**

`CreateWorkflowTemplateHandler` — invoca `WorkflowTemplate.Create(name, description)` e retorna o `Id` do template criado.

`UpdateWorkflowTemplateHandler` — carrega o aggregate, valida existência e chama `Update(name, description)`.

`ActivateWorkflowTemplateHandler` — carrega o aggregate, valida que está em `Draft` e que existe ao menos uma etapa com ao menos uma tarefa (erro: `ActivationRequiresStepWithTask`), chama `Activate()`.

`ArchiveWorkflowTemplateHandler` — carrega o aggregate, valida que o status é `Active` (erro: `InvalidStatusTransition` para qualquer outro status), chama `Archive()`.

**Handlers de step:**

`AddWorkflowTemplateStepHandler` — valida unicidade do título dentro do template (erro: `StepTitleAlreadyExists`), chama `AddStep(...)` e retorna o `StepId` gerado.

`UpdateWorkflowTemplateStepHandler` — valida existência do step e chama `UpdateStep(...)`.

`RemoveWorkflowTemplateStepHandler` — valida existência e chama `RemoveStep(stepId)`, que cascateia remoção de fields e tasks no domínio.

**Handlers de task:**

`AddWorkflowTemplateStepTaskHandler` — valida existência do step e unicidade do título dentro da etapa (erro: `TaskTitleAlreadyExists`), chama `AddTask(...)`.

`UpdateWorkflowTemplateStepTaskHandler` / `RemoveWorkflowTemplateStepTaskHandler` — mesmo padrão com validação de existência.

**Handlers de step field e task field:**

`AddTemplateStepFieldHandler` — valida existência do step e unicidade de label (erro: `StepFieldLabelAlreadyExists`), chama `AddStepField(...)` que resolve automaticamente colisões de `Order`.

`UpdateTemplateStepFieldHandler` — valida existência do step e do field (erros: `StepNotFound`, `StepFieldNotFound`), valida unicidade de label excluindo o próprio field, chama `UpdateStepField(...)`.

`RemoveTemplateStepFieldHandler` — valida existência do step e do field (erros: `StepNotFound`, `StepFieldNotFound`) antes de chamar `RemoveStepField(...)`.

`AddTemplateStepTaskFieldHandler` — valida existência do step, da task e unicidade de label (erro: `TaskFieldLabelAlreadyExists`), chama `AddTaskField(...)` que resolve automaticamente colisões de `Order`.

`UpdateTemplateStepTaskFieldHandler` — valida existência do step, task e field (erros: `StepNotFound`, `TaskNotFound`, `TaskFieldNotFound`), valida unicidade de label, chama `UpdateTaskField(...)`.

`RemoveTemplateStepTaskFieldHandler` — valida existência do step, task e field (erros: `StepNotFound`, `TaskNotFound`, `TaskFieldNotFound`) antes de chamar `RemoveTaskField(...)`.

### Por que isso importa

Os handlers são os únicos orquestradores de use case. Eles não contêm lógica de negócio (essa fica no domínio) nem lógica de persistência direta (essa fica na infraestrutura). Seu papel é coordenar: carregar, invocar e persistir.

---

## 12. Application Handlers — Queries

### Arquivos

```
src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Queries/Template/
├── GetAllWorkflowTemplatesHandler.cs
└── GetWorkflowTemplateDetailByIdHandler.cs
```

### O que fazem

**`GetAllWorkflowTemplatesHandler`:**
- Consulta a projeção `IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplate>`
- Filtra por `IsDeleted = false`
- Ordena por `Name` e depois por `CreatedAt`
- Mapeia para `List<WorkflowTemplateResponse>`

**`GetWorkflowTemplateDetailByIdHandler`:**
- Executa cinco queries em paralelo contra o read model:
  1. template pelo `Id`
  2. steps pelo `TemplateId`
  3. tasks pelo `TemplateId`
  4. step fields pelo `TemplateId`
  5. task fields pelo `TemplateId`
- Constrói a estrutura aninhada: `Template → Steps (ordenados por Order) → [Fields (ordenados), Tasks (ordenados) → [Fields (ordenados)]]`
- Retorna `WorkflowTemplateDetailResponse` completo ou `NotFoundError` se o template não existir

### Por que isso importa

As queries não tocam o event store nem reconstituem aggregates — elas leem diretamente do MongoDB. O paralelismo das cinco queries no handler de detail garante que a latência do endpoint seja a do request mais lento entre os cinco, não a soma de todos. Isso é crítico para templates grandes com muitos steps, tasks e fields.

---

## 13. Event Handlers de Projeção

### Arquivos

```
src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Events/
├── ProjectionWorkflowTemplateEventHandler.cs
├── ProjectionWorkflowTemplateStepEventHandler.cs
├── ProjectionWorkflowTemplateStepTaskEventHandler.cs
├── ProjectionTemplateStepFieldEventHandler.cs
└── ProjectionTemplateStepTaskFieldEventHandler.cs
```

### O que fazem

Cada handler de projeção recebe eventos de domínio (via MassTransit, consumidos pelos consumers da camada Web) e atualiza o read model no MongoDB com a operação correspondente.

**`ProjectionWorkflowTemplateEventHandler`:**
- `WorkflowTemplateCreated` → `ReplaceInsertAsync` (upsert completo do registro)
- `WorkflowTemplateUpdated` → `UpdateFieldsAsync` (name, description, updatedAt)
- `WorkflowTemplateActivated` → `UpdateFieldsAsync` (status = "Active", updatedAt)
- `WorkflowTemplateArchived` → `UpdateFieldsAsync` (status = "Archived", updatedAt)

**`ProjectionWorkflowTemplateStepEventHandler`:**
- `WorkflowTemplateStepAdded` → `ReplaceInsertAsync` do step
- `WorkflowTemplateStepUpdated` → `UpdateFieldsAsync` (title, description, order, updatedAt)
- `WorkflowTemplateStepRemoved` → deleta step, tasks e fields associados por ID

**`ProjectionWorkflowTemplateStepTaskEventHandler`:**
- `...TaskAdded` → `ReplaceInsertAsync` da task
- `...TaskUpdated` → `UpdateFieldsAsync`
- `...TaskRemoved` → deleta task e seus fields

**`ProjectionTemplateStepFieldEventHandler`** e **`ProjectionTemplateStepTaskFieldEventHandler`:**
- Mesmo padrão para campos de etapa e campos de tarefa respectivamente.

Todos os handlers usam try-catch com logging e lançam `InvalidOperationException` em caso de falha, forçando o consumer a reprocessar via mecanismo de retry do MassTransit.

### Por que isso importa

O read model é construído de forma assíncrona e eventual. Quando um operador ativa um template, o evento `WorkflowTemplateActivated` é publicado no RabbitMQ, o consumer o recebe e o handler de projeção atualiza o status no MongoDB. Isso desacopla a escrita (PostgreSQL, event store) da leitura (MongoDB, projections) e garante que a camada de consulta nunca precise tocar no banco transacional.

---

## 14. Validators

### Arquivos

```
src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Validators/
├── Template/
│   ├── CreateWorkflowTemplateValidator.cs
│   ├── UpdateWorkflowTemplateValidator.cs
│   ├── ActivateWorkflowTemplateValidator.cs
│   └── ArchiveWorkflowTemplateValidator.cs
├── TemplateStep/
│   ├── AddWorkflowTemplateStepValidator.cs
│   ├── UpdateWorkflowTemplateStepValidator.cs
│   └── RemoveWorkflowTemplateStepValidator.cs
├── TemplateStepTask/
│   ├── AddWorkflowTemplateStepTaskValidator.cs
│   ├── UpdateWorkflowTemplateStepTaskValidator.cs
│   └── RemoveWorkflowTemplateStepTaskValidator.cs
├── TemplateStepField/
│   ├── AddTemplateStepFieldValidator.cs
│   └── UpdateTemplateStepFieldValidator.cs
└── TemplateStepTaskField/
    ├── AddTemplateStepTaskFieldValidator.cs
    └── UpdateTemplateStepTaskFieldValidator.cs
```

### O que fazem

São 16 classes `AbstractValidator<TCommand>` usando FluentValidation. São executadas pelo pipeline do MediatR antes de qualquer handler ser chamado.

**Validators de template:**
- `Name`: obrigatório, máximo 150 caracteres
- `Description`: obrigatório, máximo 500 caracteres
- `Id` (update/activate/archive): obrigatório, `Guid` não-vazio

**Validators de step:**
- `TemplateId`, `StepId`: obrigatórios, não-vazios
- `Title`: obrigatório, máximo 200 caracteres
- `Order`: maior ou igual a 1

**Validators de task:**
- `TemplateId`, `StepId`, `TaskId`: obrigatórios
- `Title`: obrigatório
- `Type`: valor válido de `TaskType`
- `Configuration`: JSON válido quando presente

**Validators de step field e task field:**
- `Label`: obrigatório
- `FieldType`: valor válido de `FieldType`
- `Options`: obrigatório quando `FieldType = Select`

### Por que isso importa

A validação acontece na borda da camada de aplicação, antes de tocar o domínio. Isso garante que mensagens de erro de formato cheguem ao cliente como `400 Bad Request` com detalhes claros, sem desperdiçar uma viagem ao aggregate ou ao banco de dados.

---

## 15. Camada de Persistência

### Arquivos

```
src/Modules/WorkflowTemplate/WorkflowTemplate.Persistence/
├── WorkflowTemplateDbContext.cs
├── WorkflowTemplateProjectionDbContext.cs
├── IWorkflowTemplateProjectionDbContext.cs
├── Configurations/
│   ├── StoreEventConfiguration.cs
│   └── SnapshotConfiguration.cs
├── Projections/
│   ├── IWorkflowTemplateProjection.cs
│   └── WorkflowTemplateProjection.cs
├── Constants/
│   └── Schemas.cs
└── ServiceInstallers/
    └── PersistenceServiceInstaller.cs
```

### O que faz

**`WorkflowTemplateDbContext`** — event store PostgreSQL. Não tem `DbSet` de entidades de negócio; usa a base de event store do Core para armazenar eventos e snapshots do aggregate `WorkflowTemplate`. Aplica todas as configurações do assembly e usa o schema `workflow_template`.

**`WorkflowTemplateProjectionDbContext`** — read model MongoDB. Herda de `MongoDbContext` do Core. Lê a connection string de `Projections:WorkflowTemplate` da configuração.

**`IWorkflowTemplateProjectionDbContext`** — interface marcadora que estende `IMongoDbContext`, usada para injeção de dependência tipada.

**`StoreEventConfiguration`** e **`SnapshotConfiguration`** — configuram as tabelas de eventos e snapshots do aggregate no PostgreSQL, herdando de bases do Core.

**`IWorkflowTemplateProjection<TProjection>`** — interface genérica que estende `IProjection<TProjection>`. Permite que os handlers de aplicação sejam injetados com a projeção correta (`IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplate>`, por exemplo) sem depender de implementações concretas.

**`WorkflowTemplateProjection<TProjection>`** — implementação genérica que estende `Projection<TProjection>`. Recebe `IWorkflowTemplateProjectionDbContext` no construtor e fornece `ListAsync`, `GetByIdAsync`, `ReplaceInsertAsync` e `UpdateFieldsAsync`.

**`PersistenceServiceInstaller`** — registra no DI:
- `IWorkflowTemplateProjection<>` como scoped (genérico)
- `IEventStore` para `WorkflowTemplateDbContext` como scoped
- `IUnitOfWork` para `WorkflowTemplateDbContext` como scoped
- `WorkflowTemplateProjectionDbContext` com a connection string do MongoDB
- `WorkflowTemplateDbContext` com o PostgreSQL (incluindo histórico de migrations no schema)

### Por que isso importa

A separação em dois contextos (PostgreSQL para escrita, MongoDB para leitura) é a implementação física do CQRS. O event store nunca é consultado por queries de leitura — apenas por reconstituição de aggregate em operações de escrita. O read model nunca é escrito diretamente por handlers de comando — apenas por handlers de projeção via eventos.

---

## 16. Camada de Infraestrutura

### Arquivos

```
src/Modules/WorkflowTemplate/WorkflowTemplate.Infrastructure/
├── WorkflowTemplateModuleInstaller.cs
├── WorkflowTemplateApplicationService.cs
└── ServiceInstallers/
    └── (service installers específicos do módulo)
```

### O que faz

**`WorkflowTemplateModuleInstaller`** — implementa `IModuleInstaller`. É o ponto de entrada da inicialização do módulo. Registra todos os serviços do módulo no container de DI usando Scrutor para scan automático de implementações que seguem a convenção `IInterface → Implementation` nas assemblies de Infrastructure e Persistence.

**`WorkflowTemplateApplicationService`** — implementa `IWorkflowTemplateApplicationService`, que estende `IApplicationService<WorkflowTemplateDbContext>`. É a fachada de uso do aggregate pelo pipeline de comandos. Fornece:
- `LoadAggregateAsync(id)` — reconstitui o aggregate `WorkflowTemplate` a partir dos eventos do event store
- `AppendEventsAsync(aggregate)` — serializa e grava os novos eventos produzidos pelo aggregate
- `SaveChangesAsync()` — confirma a transação no `WorkflowTemplateDbContext`

### Por que isso importa

O `WorkflowTemplateApplicationService` é o único ponto de acesso ao event store a partir dos handlers de comando. Isso isola toda a lógica de reconstituição e persistência de eventos em um único lugar, evitando que handlers acessem o `DbContext` diretamente e garantindo que o ciclo load → mutate → append → save seja sempre respeitado.

---

## 17. Endpoints HTTP

### Arquivos

```
src/Web/Endpoints/Modules/WorkflowTemplate/
├── Template/
│   ├── CreateWorkflowTemplateEndpoint.cs
│   ├── UpdateWorkflowTemplateEndpoint.cs
│   ├── ActivateWorkflowTemplateEndpoint.cs
│   ├── ArchiveWorkflowTemplateEndpoint.cs
│   ├── GetAllWorkflowTemplatesEndpoint.cs
│   └── GetWorkflowTemplateDetailByIdEndpoint.cs
├── TemplateStep/
│   ├── AddWorkflowTemplateStepEndpoint.cs
│   ├── UpdateWorkflowTemplateStepEndpoint.cs
│   └── RemoveWorkflowTemplateStepEndpoint.cs
├── TemplateStepTask/
│   ├── AddWorkflowTemplateStepTaskEndpoint.cs
│   ├── UpdateWorkflowTemplateStepTaskEndpoint.cs
│   └── RemoveWorkflowTemplateStepTaskEndpoint.cs
├── TemplateStepField/
│   ├── AddTemplateStepFieldEndpoint.cs
│   ├── UpdateTemplateStepFieldEndpoint.cs
│   └── RemoveTemplateStepFieldEndpoint.cs
└── TemplateStepTaskField/
    ├── AddTemplateStepTaskFieldEndpoint.cs
    ├── UpdateTemplateStepTaskFieldEndpoint.cs
    └── RemoveTemplateStepTaskFieldEndpoint.cs

src/Web/Endpoints/Routes/WorkflowTemplateRoutes.cs
```

### O que fazem

São 18 endpoints implementados com Ardalis API Endpoints. Cada endpoint recebe um request DTO, constrói o command ou query correspondente e o envia via MediatR. A resposta passa pelo pipeline railway-oriented que mapeia `Result<T>` para o código HTTP correto.

Rota base: `v{version:apiVersion}/workflow-template`

| Método | Rota | Endpoint | Retorno |
|---|---|---|---|
| POST | `/v1/workflow-template` | CreateWorkflowTemplate | `IdentifierResponse` |
| PUT | `/v1/workflow-template/{id}` | UpdateWorkflowTemplate | 204 |
| POST | `/v1/workflow-template/{id}/activate` | ActivateWorkflowTemplate | 204 |
| POST | `/v1/workflow-template/{id}/archive` | ArchiveWorkflowTemplate | 204 |
| GET | `/v1/workflow-template` | GetAllWorkflowTemplates | `List<WorkflowTemplateResponse>` |
| GET | `/v1/workflow-template/{id}` | GetWorkflowTemplateDetailById | `WorkflowTemplateDetailResponse` |
| POST | `/v1/workflow-template/{id}/step` | AddWorkflowTemplateStep | `IdentifierResponse` |
| PUT | `/v1/workflow-template/{id}/step/{stepId}` | UpdateWorkflowTemplateStep | 204 |
| DELETE | `/v1/workflow-template/{id}/step/{stepId}` | RemoveWorkflowTemplateStep | 204 |
| POST | `/v1/workflow-template/{id}/step/{stepId}/task` | AddWorkflowTemplateStepTask | `IdentifierResponse` |
| PUT | `/v1/workflow-template/{id}/step/{stepId}/task/{taskId}` | UpdateWorkflowTemplateStepTask | 204 |
| DELETE | `/v1/workflow-template/{id}/step/{stepId}/task/{taskId}` | RemoveWorkflowTemplateStepTask | 204 |
| POST | `/v1/workflow-template/{id}/step/{stepId}/field` | AddTemplateStepField | `IdentifierResponse` |
| PUT | `/v1/workflow-template/{id}/step/{stepId}/field/{fieldId}` | UpdateTemplateStepField | 204 |
| DELETE | `/v1/workflow-template/{id}/step/{stepId}/field/{fieldId}` | RemoveTemplateStepField | 204 |
| POST | `/v1/workflow-template/{id}/step/{stepId}/task/{taskId}/field` | AddTemplateStepTaskField | `IdentifierResponse` |
| PUT | `/v1/workflow-template/{id}/step/{stepId}/task/{taskId}/field/{fieldId}` | UpdateTemplateStepTaskField | 204 |
| DELETE | `/v1/workflow-template/{id}/step/{stepId}/task/{taskId}/field/{fieldId}` | RemoveTemplateStepTaskField | 204 |

### Por que isso importa

A camada de endpoints é a interface pública da API. Ela é deliberadamente fina — sem lógica de negócio, sem acesso direto ao banco. Seu único papel é mapear HTTP para MediatR e MediatR para HTTP, respeitando convenções REST e devolvendo códigos semânticos.

---

## 18. Consumers MassTransit

### Arquivos

```
src/Web/Endpoints/Modules/WorkflowTemplate/Consumers/
├── ProjectionWorkflowTemplateConsumer.cs
├── ProjectionWorkflowTemplateStepConsumer.cs
├── ProjectionWorkflowTemplateStepTaskConsumer.cs
├── ProjectionTemplateStepFieldConsumer.cs
├── ProjectionTemplateStepTaskFieldConsumer.cs
└── ConsumerConfiguration.cs
```

### O que fazem

Cada consumer implementa `IConsumer<T>` do MassTransit para um ou mais eventos de domínio. Quando o event store publica um evento no RabbitMQ (via `IEventBus`), o consumer correspondente o recebe e delega para o handler de projeção da camada Application.

**`ProjectionWorkflowTemplateConsumer`** — consome os 4 eventos de template (`Created`, `Updated`, `Activated`, `Archived`) e delega para `IProjectionWorkflowTemplateEventHandler`.

**`ProjectionWorkflowTemplateStepConsumer`** — consome os 3 eventos de step (`Added`, `Updated`, `Removed`).

**`ProjectionWorkflowTemplateStepTaskConsumer`** — consome os 3 eventos de task.

**`ProjectionTemplateStepFieldConsumer`** — consome os 3 eventos de step field.

**`ProjectionTemplateStepTaskFieldConsumer`** — consome os 3 eventos de task field.

**`ConsumerConfiguration`** — registra todos os consumers no MassTransit durante a inicialização da aplicação.

### Por que isso importa

Os consumers são a ponte entre o write model e o read model. Um comando que ativa um template gera um evento no PostgreSQL, o event bus o publica no RabbitMQ e o consumer o consome para atualizar o MongoDB. Esse fluxo é totalmente assíncrono e desacoplado — a operação de escrita retorna ao cliente sem esperar a projeção terminar. O MassTransit gerencia retries e dead-letter queues em caso de falha no consumer.

---

## 19. Testes Automatizados

### Arquivos

```
tests/Web.Tests/Modules/WorkflowTemplate/
├── Domain/
│   ├── Aggregates/
│   │   └── WorkflowTemplateAggregateTests.cs
│   └── WorkflowTemplateFieldBehaviorTests.cs
└── Application/
    ├── CreateWorkflowTemplateHandlerTests.cs
    ├── UpdateWorkflowTemplateHandlerTests.cs
    ├── ActivateWorkflowTemplateHandlerTests.cs
    ├── ArchiveWorkflowTemplateHandlerTests.cs
    ├── AddWorkflowTemplateStepHandlerTests.cs
    ├── UpdateWorkflowTemplateStepHandlerTests.cs
    ├── RemoveWorkflowTemplateStepHandlerTests.cs
    ├── AddWorkflowTemplateStepTaskHandlerTests.cs
    ├── UpdateWorkflowTemplateStepTaskHandlerTests.cs
    ├── RemoveWorkflowTemplateStepTaskHandlerTests.cs
    ├── AddTemplateStepFieldHandlerTests.cs
    ├── UpdateTemplateStepFieldHandlerTests.cs
    ├── RemoveTemplateStepFieldHandlerTests.cs
    ├── AddTemplateStepTaskFieldHandlerTests.cs
    ├── GetAllWorkflowTemplatesHandlerTests.cs
    ├── GetWorkflowTemplateDetailByIdHandlerTests.cs
    ├── ProjectionWorkflowTemplateEventHandlerTests.cs
    ├── ProjectionWorkflowTemplateStepEventHandlerTests.cs
    ├── ProjectionWorkflowTemplateStepTaskEventHandlerTests.cs
    ├── ProjectionTemplateStepFieldEventHandlerTests.cs
    └── ProjectionTemplateStepTaskFieldEventHandlerTests.cs
```

### O que cobrem

**Testes de domínio:**
- `WorkflowTemplateAggregateTests` — verifica criação, update, activate (com e sem steps/tasks), archive, adição e remoção de steps/tasks/fields, geração correta de eventos, e reconstituição de estado via `ApplyEvent`
- `WorkflowTemplateFieldBehaviorTests` — testa comportamentos específicos de fields: unicidade de label, ordem, tipos e cascata de remoção

**Testes de application — handlers de comando:**
- Um arquivo de teste por handler
- Padrão AAA (Arrange / Act / Assert) com mocks de `IWorkflowTemplateApplicationService` e `IWorkflowTemplateProjection`
- Verificam o caminho feliz, erros de not found, erros de conflito e casos de borda

**Testes de application — handlers de query:**
- `GetAllWorkflowTemplatesHandlerTests` — verifica filtro por `IsDeleted`, ordenação e mapeamento para DTO
- `GetWorkflowTemplateDetailByIdHandlerTests` — verifica montagem da estrutura aninhada, ordenação por `Order` em cada nível e retorno de `NotFoundError`

**Testes de event handlers:**
- Um arquivo por handler de projeção
- Verificam que o método correto do `IWorkflowTemplateProjection` é chamado com o payload esperado para cada evento

### Por que isso importa

A suíte de testes protege o comportamento do módulo contra regressões em todas as camadas. Testes de domínio verificam as regras de negócio independentemente de infraestrutura. Testes de handlers verificam a orquestração sem banco de dados real. Testes de projeção verificam o mapeamento evento → read model sem RabbitMQ. Juntos, eles garantem que qualquer mudança no módulo seja detectada antes de chegar ao ambiente de produção.

---

*Atualizado em 04/04/2026.*