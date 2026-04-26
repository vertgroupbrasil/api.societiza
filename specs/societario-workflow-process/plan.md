# Plano — societario-workflow-process

**Spec**: specs/societario-workflow-process/spec.md @ product.societiza
**Modulos afetados**: WorkflowProcess (novo), Web
**Modulo novo?**: sim — WorkflowProcess (5 projetos: Domain, Application, Infrastructure, Persistence, Shared)
**Resumo**: Cria o modulo WorkflowProcess — board operacional kanban onde o contador executa processos societarios (abertura, alteracao, baixa). O processo nasce a partir de uma fotografia do template ativo da contabilidade, materializa steps/tasks/fields como instancias runtime, e avanca automaticamente conforme tarefas sao concluidas ou saltadas.

---

## Pre-condicoes

1. **Template ativo** — deve existir pelo menos um WorkflowTemplate com status `Active` na contabilidade para que um processo possa ser criado. O handler valida isso.
2. **`appliesTo` ja existe no template** — `WorkflowTemplateStep.AppliesTo` e `List<string>` com os nomes dos tipos de processo aos quais a etapa se aplica (ex: `["Abertura", "Baixa"]`). Lista vazia = aplica a todos. O filtro por `AppliesTo` e aplicado em `WorkflowTemplateDataService` (Fase 6 · #1) ao montar os snapshots — apenas steps aplicaveis ao tipo de processo sao incluidos. **Tudo que foi materializado e obrigatorio para conclusao** — `IsRequired` nao existe em `ProcessStepInstance`.
3. **`IsRequired` nao existe em campos do template** — a spec menciona campo obrigatorio em `ProcessStepFieldInstance` e `ProcessStepTaskFieldInstance`, mas o template nao tem essa informacao. Para o MVP, **todos os campos sao opcionais** (`IsRequired = false`). Nao bloqueia avanço.

---

## Fases de implementacao

---

### Fase 0 — Scaffold

**Referencia**: `.claude/skills/architecture/SKILL.md` (estrutura de modulos)

#### 1. Scaffold do modulo WorkflowProcess — 5 projetos .csproj

- **Camada**: todas
- **Tipo**: ModuleScaffold
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/` (5 projetos)
- **Depende de**: nenhum
- **Spec-ref**: §5 "feature core — peca central do MVP"
- **Detalhes**: Criar os 5 projetos seguindo a mesma estrutura do WorkflowTemplate:
  - `WorkflowProcess.Domain` — ref: Core.Domain
  - `WorkflowProcess.Shared` — ref: Core.Shared
  - `WorkflowProcess.Application` — ref: Domain, Shared, Core.Application
  - `WorkflowProcess.Persistence` — ref: Domain, Core.Persistence
  - `WorkflowProcess.Infrastructure` — ref: Application, Persistence, Core.Infrastructure
  - Cada projeto inclui `AssemblyReference.cs`
  - Adicionar todos ao `Societiza.sln`

**Build gate**: `dotnet build Societiza.sln` — solucao carrega sem erros de referencia.

---

### Fase 1 — Domain

**Referencia**: `references/domain-architecture.md`

#### 1. SmartEnums: ProcessType, ProcessStatus, StepInstanceStatus, TaskInstanceStatus

- **Camada**: Domain
- **Tipo**: SmartEnum
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Domain/Enumerations/`
- **Depende de**: nenhum
- **Spec-ref**: §8 "Tipos de processo no MVP", "Estado do Process", "Campos da ProcessStepInstance", "Campos da ProcessStepTaskInstance"
- **Detalhes**:
  - `ProcessType`: Abertura(0), Alteracao(1), Baixa(2) — value object semantico; `abstract class` + `SmartEnum<ProcessType>`
  - `ProcessStatus`: InProgress(0), Completed(1)
  - `StepInstanceStatus`: NotStarted(0), InProgress(1), Completed(2)
  - `TaskInstanceStatus`: Pending(0), Completed(1), Skipped(2)
  - Todos seguem o pattern: implicit string/int conversions, private sealed subclasses, `[method: JsonConstructor]`

#### 2. Entity: ProcessStepFieldInstance

- **Camada**: Domain
- **Tipo**: Entity
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Domain/Entities/ProcessStepFieldInstance.cs`
- **Depende de**: nenhum
- **Spec-ref**: §8 "ProcessStepFieldInstance (campos de etapa)"
- **Detalhes**: Materializacao runtime de TemplateStepField. Props: StepInstanceId, TemplateFieldId, Label, FieldType (string), Options (string?), IsRequired (bool, default false), Value (string?), Order (int), FilledAt (DateTimeOffset?). Metodos: Fill(value), Update via When().

#### 3. Entity: ProcessStepTaskFieldInstance

- **Camada**: Domain
- **Tipo**: Entity
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Domain/Entities/ProcessStepTaskFieldInstance.cs`
- **Depende de**: nenhum
- **Spec-ref**: §8 "ProcessStepTaskFieldInstance (campos de tarefa)"
- **Detalhes**: Mesma estrutura do #2 mas vinculado a TaskInstance. Props: TaskInstanceId, TemplateFieldId, Label, FieldType (string), Options (string?), IsRequired (bool, default false), Value (string?), Order (int), FilledAt (DateTimeOffset?).

#### 4. Entity: ProcessStepTaskInstance

- **Camada**: Domain
- **Tipo**: Entity
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Domain/Entities/ProcessStepTaskInstance.cs`
- **Depende de**: #1, #3
- **Spec-ref**: §8 "Campos da ProcessStepTaskInstance", "Salto de tarefa", "Auditoria de tarefa"
- **Detalhes**: Materializacao runtime de WorkflowTemplateStepTask. Props: StepInstanceId, TemplateTaskId, Title, Order, IsOptional (bool, copiado do template), Status (TaskInstanceStatus), Fields (List<ProcessStepTaskFieldInstance>), CompletedAt, SkippedAt. Metodos internos: `MarkCompleted(DateTimeOffset)`, `MarkSkipped(DateTimeOffset)`, AddField(). **MVP nao rastreia quem completou/saltou** — CompletedBy/SkippedBy removidos.

#### 5. Entity: ProcessStepInstance

- **Camada**: Domain
- **Tipo**: Entity
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Domain/Entities/ProcessStepInstance.cs`
- **Depende de**: #1, #2, #4
- **Spec-ref**: §8 "Campos da ProcessStepInstance", "Avanco de etapa"
- **Detalhes**: Materializacao runtime de WorkflowTemplateStep. Props: ProcessId, TemplateStepId, Title, Order, Status (StepInstanceStatus), Tasks (List<ProcessStepTaskInstance>), Fields (List<ProcessStepFieldInstance>), CompletedAt. Metodos internos: Start(), Complete(), AddTask(), AddField(). Helper: `AreAllRequiredTasksResolved()` — verifica se todas as tasks nao-opcionais estao Completed ou Skipped. **`IsRequired` removido** — o filtro `AppliesTo` garante que so steps aplicaveis sao materializados; tudo materializado e obrigatorio.

#### 6. Domain Events

- **Camada**: Domain
- **Tipo**: DomainEvent
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Domain/Events/DomainEvents.cs`
- **Depende de**: #1
- **Spec-ref**: §8 todas as decisoes de comportamento
- **Detalhes**: Arquivo unico com todos os eventos do modulo:
  - `WorkflowProcessCreated(Id, AccountancyId, TemplateId, TemplateVersion, ProcessType, TargetClient, Status, CreatedAt, Version)`
  - `ProcessStepInstanceMaterialized(ProcessId, StepInstanceId, TemplateStepId, Title, Order, Status, CreatedAt, Version)` — **sem IsRequired**
  - `ProcessStepTaskInstanceMaterialized(ProcessId, StepInstanceId, TaskInstanceId, TemplateTaskId, Title, Order, IsOptional, Status, CreatedAt, Version)`
  - `ProcessStepFieldInstanceMaterialized(ProcessId, StepInstanceId, FieldInstanceId, TemplateFieldId, Label, FieldType, Options, IsRequired, Order, CreatedAt, Version)`
  - `ProcessStepTaskFieldInstanceMaterialized(ProcessId, StepInstanceId, TaskInstanceId, FieldInstanceId, TemplateFieldId, Label, FieldType, Options, IsRequired, Order, CreatedAt, Version)`
  - `ProcessStepTaskInstanceCompleted(ProcessId, StepInstanceId, TaskInstanceId, CompletedAt, Version)` — **sem CompletedBy**
  - `ProcessStepTaskInstanceSkipped(ProcessId, StepInstanceId, TaskInstanceId, SkippedAt, Version)` — **sem SkippedBy**
  - `ProcessStepInstanceStarted(ProcessId, StepInstanceId, UpdatedAt, Version)`
  - `ProcessStepInstanceCompleted(ProcessId, StepInstanceId, CompletedAt, Version)`
  - `WorkflowProcessCompleted(ProcessId, CompletedAt, Version)`
  - `ProcessStepFieldInstanceFilled(ProcessId, StepInstanceId, FieldInstanceId, Value, FilledAt, Version)`
  - `ProcessStepTaskFieldInstanceFilled(ProcessId, StepInstanceId, TaskInstanceId, FieldInstanceId, Value, FilledAt, Version)`
  - Todos: `sealed record`, herdam `Message, IDomainEvent`, campos primitivos apenas, `Version` obrigatorio

#### 7. Aggregate: WorkflowProcess

- **Camada**: Domain
- **Tipo**: Aggregate
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Domain/Aggregates/WorkflowProcess.cs`
- **Depende de**: #1, #2, #3, #4, #5, #6
- **Spec-ref**: §8 "Campos do Process", "Avanco de etapa", "Criterio de conclusao do Process"
- **Detalhes**:
  - Props: AccountancyId, TemplateId, TemplateVersion (string), ProcessType, TargetClient (string), Status (ProcessStatus), Steps (List<ProcessStepInstance>), CompletedAt
  - **Create()**: factory method — recebe dados do cabecalho, levanta `WorkflowProcessCreated`. Nao materializa steps — o handler orquestra chamando metodos de materializacao.
  - **MaterializeStep(...)**: levanta `ProcessStepInstanceMaterialized` (sem IsRequired), retorna stepInstanceId
  - **MaterializeTask(...)**: levanta `ProcessStepTaskInstanceMaterialized`, retorna taskInstanceId
  - **MaterializeStepField(...)**: levanta `ProcessStepFieldInstanceMaterialized`
  - **MaterializeTaskField(...)**: levanta `ProcessStepTaskFieldInstanceMaterialized`
  - **CompleteTask(stepInstanceId, taskInstanceId)**: levanta `ProcessStepTaskInstanceCompleted`. Em cascata: se step era NotStarted → levanta `ProcessStepInstanceStarted`; se todas tasks nao-opcionais resolvidas → levanta `ProcessStepInstanceCompleted`; se **todos** steps completos → levanta `WorkflowProcessCompleted`.
  - **SkipTask(stepInstanceId, taskInstanceId)**: mesma logica de cascata que CompleteTask, levanta `ProcessStepTaskInstanceSkipped`.
  - **FillStepField(stepInstanceId, fieldInstanceId, value)**: levanta `ProcessStepFieldInstanceFilled`
  - **FillTaskField(stepInstanceId, taskInstanceId, fieldInstanceId, value)**: levanta `ProcessStepTaskFieldInstanceFilled`
  - **ApplyEvent()** → `When(@event as dynamic)` com um `When()` por evento

#### 8. Projection Models

- **Camada**: Domain
- **Tipo**: ProjectionModel
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Domain/Projections/ProjectionModel.cs`
- **Depende de**: nenhum
- **Spec-ref**: §4 "board com tipo, etapa atual, tarefas pendentes"
- **Detalhes**: `static class ProjectionModel` com 5 records:
  - `WorkflowProcess(Id, AccountancyId, TemplateId, TemplateVersion, ProcessType, TargetClient, Status, CreatedAt, CompletedAt?, UpdatedAt?, IsDeleted)`
  - `ProcessStepInstance(Id, ProcessId, TemplateStepId, Title, Order, Status, CreatedAt, CompletedAt?, UpdatedAt?)` — **sem IsRequired**
  - `ProcessStepTaskInstance(Id, ProcessId, StepInstanceId, TemplateTaskId, Title, Order, IsOptional, Status, CompletedAt?, SkippedAt?, CreatedAt, UpdatedAt?)` — **sem CompletedBy/SkippedBy**
  - `ProcessStepFieldInstance(Id, ProcessId, StepInstanceId, TemplateFieldId, Label, FieldType, Options?, IsRequired, Order, Value?, FilledAt?, CreatedAt)`
  - `ProcessStepTaskFieldInstance(Id, ProcessId, StepInstanceId, TaskInstanceId, TemplateFieldId, Label, FieldType, Options?, IsRequired, Order, Value?, FilledAt?, CreatedAt)`
  - Todos implementam `IProjectionModel`, campos primitivos

**Build gate**: `dotnet build Societiza.sln` — zero errors antes de avançar.

---

### Fase 2 — Shared

**Referencia**: `references/shared-architecture.md`

#### 1. Template Snapshot DTOs

- **Camada**: Shared
- **Tipo**: DTO
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Shared/Snapshots/`
- **Depende de**: nenhum
- **Spec-ref**: §8 "Versionamento do Process — fotografia"
- **Detalhes**: Records que representam a fotografia do template no momento da criacao do processo. Vivem em Shared para que Application (via `IWorkflowTemplateDataService`) e a implementacao cross-module na Web layer possam usa-los sem duplicacao. Nenhuma referencia a WorkflowTemplate.Domain.
  - `TemplateStepSnapshot(Guid StepId, string Title, string Description, int Order, List<TemplateStepTaskSnapshot> Tasks, List<TemplateStepFieldSnapshot> Fields)`
  - `TemplateStepTaskSnapshot(Guid TaskId, string Title, string Description, string Type, string Configuration, int Order, bool IsOptional, List<TemplateStepTaskFieldSnapshot> Fields)`
  - `TemplateStepFieldSnapshot(Guid FieldId, string Label, string FieldType, string? Options, int Order)`
  - `TemplateStepTaskFieldSnapshot(Guid FieldId, string Label, string FieldType, string? Options, int Order)`

#### 2. Command: CreateWorkflowProcessCommand

- **Camada**: Shared
- **Tipo**: Command
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Shared/Commands/CreateWorkflowProcessCommand.cs`
- **Depende de**: nenhum
- **Spec-ref**: §8 "Criacao do processo"
- **Detalhes**: `sealed record CreateWorkflowProcessCommand(Guid AccountancyId, Guid TemplateId, string ProcessType, string TargetClient) : ICommand<IdentifierResponse>`. **Nao carrega `Steps` nem `TemplateVersion`** — o handler busca esses dados internamente via `IWorkflowTemplateDataService`. O endpoint fica limpo: apenas os dados que o usuario fornece.

#### 3. Command: CompleteProcessTaskCommand

- **Camada**: Shared
- **Tipo**: Command
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Shared/Commands/CompleteProcessTaskCommand.cs`
- **Depende de**: nenhum
- **Spec-ref**: §8 "Avanco de etapa", "Auditoria de tarefa"
- **Detalhes**: `sealed record CompleteProcessTaskCommand(Guid ProcessId, Guid StepInstanceId, Guid TaskInstanceId) : ICommand` — **sem CompletedBy**

#### 4. Command: SkipProcessTaskCommand

- **Camada**: Shared
- **Tipo**: Command
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Shared/Commands/SkipProcessTaskCommand.cs`
- **Depende de**: nenhum
- **Spec-ref**: §8 "Salto de tarefa"
- **Detalhes**: `sealed record SkipProcessTaskCommand(Guid ProcessId, Guid StepInstanceId, Guid TaskInstanceId) : ICommand` — **sem SkippedBy**

#### 5. Command: FillProcessStepFieldCommand

- **Camada**: Shared
- **Tipo**: Command
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Shared/Commands/FillProcessStepFieldCommand.cs`
- **Depende de**: nenhum
- **Spec-ref**: §8 "ProcessStepFieldInstance"
- **Detalhes**: `sealed record FillProcessStepFieldCommand(Guid ProcessId, Guid StepInstanceId, Guid FieldInstanceId, string Value) : ICommand`

#### 6. Command: FillProcessTaskFieldCommand

- **Camada**: Shared
- **Tipo**: Command
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Shared/Commands/FillProcessTaskFieldCommand.cs`
- **Depende de**: nenhum
- **Spec-ref**: §8 "ProcessStepTaskFieldInstance"
- **Detalhes**: `sealed record FillProcessTaskFieldCommand(Guid ProcessId, Guid StepInstanceId, Guid TaskInstanceId, Guid FieldInstanceId, string Value) : ICommand`

#### 7. Queries: GetProcessBoardQuery + GetProcessDetailQuery

- **Camada**: Shared
- **Tipo**: Query
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Shared/Queries/`
- **Depende de**: nenhum
- **Spec-ref**: §1 "board operacional", §8 "Board unico por contabilidade", "Filtragem por tipo"
- **Detalhes**:
  - `GetProcessBoardQuery(Guid AccountancyId, string? ProcessType) : IQuery<List<ProcessBoardItemResponse>>` — aceita filtro opcional por tipo
  - `GetProcessDetailQuery(Guid ProcessId) : IQuery<ProcessDetailResponse>`

#### 8. Response DTOs

- **Camada**: Shared
- **Tipo**: ResponseDTO
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Shared/Response/`
- **Depende de**: nenhum
- **Spec-ref**: §4 "board com tipo, etapa atual, tarefas pendentes", §8
- **Detalhes**:
  - `ProcessBoardItemResponse(Id, ProcessType, TargetClient, Status, CurrentStepTitle, TotalSteps, CompletedSteps, CreatedAt, CompletedAt?)`
  - `ProcessDetailResponse(Id, AccountancyId, TemplateId, TemplateVersion, ProcessType, TargetClient, Status, CreatedAt, CompletedAt?, List<ProcessStepInstanceResponse> Steps)`
  - `ProcessStepInstanceResponse(Id, Title, Order, Status, CompletedAt?, List<ProcessStepTaskInstanceResponse> Tasks, List<ProcessStepFieldInstanceResponse> Fields)` — **sem IsRequired**
  - `ProcessStepTaskInstanceResponse(Id, Title, Order, IsOptional, Status, CompletedAt?, SkippedAt?, List<ProcessStepTaskFieldInstanceResponse> Fields)` — **sem CompletedBy/SkippedBy**
  - `ProcessStepFieldInstanceResponse(Id, Label, FieldType, Options?, IsRequired, Order, Value?, FilledAt?)`
  - `ProcessStepTaskFieldInstanceResponse(Id, Label, FieldType, Options?, IsRequired, Order, Value?, FilledAt?)`

**Build gate**: `dotnet build Societiza.sln` — zero errors antes de avançar.

---

### Fase 3 — Application

**Referencia**: `references/application-architecture.md`

#### 1. IWorkflowProcessApplicationService

- **Camada**: Application
- **Tipo**: Service interface
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Application/Services/IWorkflowProcessApplicationService.cs`
- **Depende de**: nenhum
- **Spec-ref**: pattern de arquitetura
- **Detalhes**: `public interface IWorkflowProcessApplicationService : IApplicationService<WorkflowProcessDbContext> { }` — marker interface para DI tipado do modulo

#### 2. IWorkflowTemplateDataService — interface anti-corrupcao cross-module

- **Camada**: Application
- **Tipo**: Service interface
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Application/Services/IWorkflowTemplateDataService.cs`
- **Depende de**: Fase 2 · #1
- **Spec-ref**: §8 "Versionamento do Process — fotografia", "Materializacao por tipo"
- **Detalhes**: Interface definida em Application. A implementacao concreta vive na Web layer (composition root), que pode enxergar ambos os modulos sem criar dependencia entre eles.
  ```csharp
  public interface IWorkflowTemplateDataService
  {
      // Retorna null se o template nao existir ou nao estiver Active.
      // processType e usado para filtrar steps por AppliesTo.
      Task<(string Version, List<TemplateStepSnapshot> Steps)?> GetActiveTemplateSnapshotAsync(
          Guid templateId, string processType, CancellationToken ct);
  }
  ```
  A implementacao despacha `GetWorkflowTemplateDetailByIdQuery` via MediatR — nao acessa MongoDB diretamente. O registro e feito em Fase 6 · #7.

#### 3. Validators

- **Camada**: Application
- **Tipo**: Validator
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Application/UseCases/Validators/`
- **Depende de**: Fase 1 · #1, Fase 2 · #2, #3, #4, #5, #6
- **Spec-ref**: §8
- **Detalhes**: 5 validators, um por write command:
  - `CreateWorkflowProcessValidator` — NotEmpty: AccountancyId, TemplateId, TargetClient; ProcessType valido via `ProcessType.TryFromName`
  - `CompleteProcessTaskValidator` — NotEmpty: ProcessId, StepInstanceId, TaskInstanceId
  - `SkipProcessTaskValidator` — NotEmpty: ProcessId, StepInstanceId, TaskInstanceId
  - `FillProcessStepFieldValidator` — NotEmpty: ProcessId, StepInstanceId, FieldInstanceId, Value
  - `FillProcessTaskFieldValidator` — NotEmpty: ProcessId, StepInstanceId, TaskInstanceId, FieldInstanceId, Value

#### 4. CreateWorkflowProcessHandler

- **Camada**: Application
- **Tipo**: Handler
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Application/UseCases/Commands/CreateWorkflowProcessHandler.cs`
- **Depende de**: #1, #2, Fase 1 · #7, Fase 2 · #2
- **Spec-ref**: §8 "Criacao do processo", "Versionamento do Process", "Materializacao por tipo"
- **Detalhes**: `ICommandHandler<CreateWorkflowProcessCommand, IdentifierResponse>`. Injeta `IWorkflowProcessApplicationService` e `IWorkflowTemplateDataService`. Fluxo:
  1. Chama `_templateDataService.GetActiveTemplateSnapshotAsync(command.TemplateId, command.ProcessType, ct)`
  2. Se retornar `null` → `Result.Failure(WorkflowProcessErrors.TemplateNotActiveOrNotFound)`
  3. Cria o aggregate via `WorkflowProcess.Create(command.AccountancyId, command.TemplateId, template.Version, command.ProcessType, command.TargetClient)`
  4. Itera sobre `template.Steps` chamando `process.MaterializeStep(...)` para cada step
  5. Para cada step, itera tasks chamando `process.MaterializeTask(...)` e fields chamando `process.MaterializeStepField(...)`
  6. Para cada task, itera fields chamando `process.MaterializeTaskField(...)`
  7. `AppendEventsAsync(process)` — persiste todos os eventos de uma vez
  8. Retorna `IdentifierResponse(process.Id)`

#### 5. CompleteProcessTaskHandler

- **Camada**: Application
- **Tipo**: Handler
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Application/UseCases/Commands/CompleteProcessTaskHandler.cs`
- **Depende de**: #1, Fase 1 · #7, Fase 2 · #3
- **Spec-ref**: §8 "Avanco de etapa", "Auditoria de tarefa", "Criterio de conclusao do Process"
- **Detalhes**: `ICommandHandler<CompleteProcessTaskCommand>`. Fluxo:
  1. `LoadAggregateAsync<WorkflowProcess>(command.ProcessId)`
  2. Valida: processo nao esta Completed (ConflictError)
  3. Valida: step e task existem no aggregate (NotFoundError)
  4. Valida: task nao esta ja Completed ou Skipped (ConflictError)
  5. `process.CompleteTask(stepInstanceId, taskInstanceId)` — cascata automatica
  6. `AppendEventsAsync(process)`

#### 6. SkipProcessTaskHandler

- **Camada**: Application
- **Tipo**: Handler
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Application/UseCases/Commands/SkipProcessTaskHandler.cs`
- **Depende de**: #1, Fase 1 · #7, Fase 2 · #4
- **Spec-ref**: §8 "Salto de tarefa"
- **Detalhes**: Mesma estrutura do #5, mas chama `process.SkipTask(...)`.

#### 7. FillProcessStepFieldHandler + FillProcessTaskFieldHandler

- **Camada**: Application
- **Tipo**: Handler
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Application/UseCases/Commands/`
- **Depende de**: #1, Fase 1 · #7, Fase 2 · #5, #6
- **Spec-ref**: §8 "ProcessStepFieldInstance", "ProcessStepTaskFieldInstance"
- **Detalhes**: Dois handlers simples. Cada um:
  1. Carrega aggregate
  2. Valida que processo nao esta Completed
  3. Valida que step/field existem
  4. Chama `process.FillStepField(...)` ou `process.FillTaskField(...)`
  5. Persiste

#### 8. GetProcessBoardHandler

- **Camada**: Application
- **Tipo**: QueryHandler
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Application/UseCases/Queries/GetProcessBoardHandler.cs`
- **Depende de**: Fase 1 · #8, Fase 2 · #7, #8
- **Spec-ref**: §1 "board operacional", §8 "Filtragem por tipo"
- **Detalhes**: `IQueryHandler<GetProcessBoardQuery, List<ProcessBoardItemResponse>>`. Injeta `IWorkflowProcessProjection<ProjectionModel.WorkflowProcess>` e `IWorkflowProcessProjection<ProjectionModel.ProcessStepInstance>`. Fluxo:
  1. Lista processos por AccountancyId (e ProcessType se fornecido) via projection
  2. Para cada processo, calcula TotalSteps, CompletedSteps e CurrentStepTitle a partir das step projections
  3. Monta `ProcessBoardItemResponse`

#### 9. GetProcessDetailHandler

- **Camada**: Application
- **Tipo**: QueryHandler
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Application/UseCases/Queries/GetProcessDetailHandler.cs`
- **Depende de**: Fase 1 · #8, Fase 2 · #7, #8
- **Spec-ref**: §4 "tipo explicito, etapa atual, tarefas pendentes e campos preenchidos"
- **Detalhes**: `IQueryHandler<GetProcessDetailQuery, ProcessDetailResponse>`. Injeta projections de todos os 5 models. Fluxo:
  1. Busca processo por Id
  2. Busca steps, tasks, step fields, task fields relacionados
  3. Monta arvore completa: ProcessDetailResponse com Steps aninhados, cada um com Tasks e Fields

#### 10. Projection Handlers

- **Camada**: Application
- **Tipo**: ProjectionHandler
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Application/UseCases/Events/`
- **Depende de**: Fase 1 · #6, #8
- **Spec-ref**: pattern de arquitetura
- **Detalhes**: 5 projection handlers, um por projection model:
  - `ProjectionWorkflowProcessEventHandler` — consome: WorkflowProcessCreated, WorkflowProcessCompleted. Interface: `IProjectionWorkflowProcessEventHandler`.
  - `ProjectionProcessStepInstanceEventHandler` — consome: ProcessStepInstanceMaterialized, ProcessStepInstanceStarted, ProcessStepInstanceCompleted. Interface: `IProjectionProcessStepInstanceEventHandler`.
  - `ProjectionProcessStepTaskInstanceEventHandler` — consome: ProcessStepTaskInstanceMaterialized, ProcessStepTaskInstanceCompleted, ProcessStepTaskInstanceSkipped. Interface: `IProjectionProcessStepTaskInstanceEventHandler`.
  - `ProjectionProcessStepFieldInstanceEventHandler` — consome: ProcessStepFieldInstanceMaterialized, ProcessStepFieldInstanceFilled. Interface: `IProjectionProcessStepFieldInstanceEventHandler`.
  - `ProjectionProcessStepTaskFieldInstanceEventHandler` — consome: ProcessStepTaskFieldInstanceMaterialized, ProcessStepTaskFieldInstanceFilled. Interface: `IProjectionProcessStepTaskFieldInstanceEventHandler`.
  - Pattern padrao: creation → `ReplaceInsertAsync(new(...))`, update → `FindAsync` + `existing with { ... }` + `ReplaceInsertAsync`
  - try/catch com log + rethrow `InvalidOperationException`

#### 11. ServiceCollectionExtensions (AddEventInteractors)

- **Camada**: Application
- **Tipo**: DI registration
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Application/DependencyInjection/ServiceCollectionExtensions.cs`
- **Depende de**: #10
- **Spec-ref**: pattern de arquitetura
- **Detalhes**: Registro manual dos 5 projection handlers:
  ```csharp
  services.AddScoped<IProjectionWorkflowProcessEventHandler, ProjectionWorkflowProcessEventHandler>()
          .AddScoped<IProjectionProcessStepInstanceEventHandler, ProjectionProcessStepInstanceEventHandler>()
          .AddScoped<IProjectionProcessStepTaskInstanceEventHandler, ProjectionProcessStepTaskInstanceEventHandler>()
          .AddScoped<IProjectionProcessStepFieldInstanceEventHandler, ProjectionProcessStepFieldInstanceEventHandler>()
          .AddScoped<IProjectionProcessStepTaskFieldInstanceEventHandler, ProjectionProcessStepTaskFieldInstanceEventHandler>();
  ```

**Build gate**: `dotnet build Societiza.sln` — zero errors antes de avançar.

---

### Fase 4 — Persistence

**Referencia**: `references/persistence-architecture.md`

#### 1. Persistence scaffold

- **Camada**: Persistence
- **Tipo**: DbContext + ProjectionContext + interfaces
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Persistence/`
- **Depende de**: Fase 1 · #8
- **Spec-ref**: pattern de arquitetura
- **Detalhes**:
  - `Constants/Schemas.cs` — `internal static class Schemas { internal const string WorkflowProcess = "workflow_process"; }`
  - `Context/WorkflowProcessDbContext.cs` — herda DbContext, schema `workflow_process`, aplica configs do assembly
  - `Context/Interfaces/IWorkflowProcessProjectionDbContext.cs` — marker interface
  - `Context/WorkflowProcessProjectionDbContext.cs` — herda MongoDbContext, connection string de `Projections:WorkflowProcess`
  - `Projection/IWorkflowProcessProjection.cs` — `IWorkflowProcessProjection<T> : IProjection<T>`
  - `Projection/WorkflowProcessProjection.cs` — `WorkflowProcessProjection<T>(IWorkflowProcessProjectionDbContext) : Projection<T>, IWorkflowProcessProjection<T>`

#### 2. EF Configurations

- **Camada**: Persistence
- **Tipo**: EFConfiguration
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Persistence/Configurations/`
- **Depende de**: Fase 1 · #7, #1
- **Spec-ref**: pattern de arquitetura
- **Detalhes**:
  - `WorkflowProcessStoreEventConfiguration : StoreEventConfiguration<WorkflowProcess>` — classe vazia
  - `WorkflowProcessSnapshotConfiguration : SnapshotConfiguration<WorkflowProcess>` — classe vazia

#### 3. Migration inicial

- **Camada**: Persistence
- **Tipo**: Migration
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Persistence/Migrations/`
- **Depende de**: #1, #2
- **Spec-ref**: pattern de arquitetura
- **Detalhes**: `dotnet ef migrations add InitialCreate --project src/Modules/WorkflowProcess/WorkflowProcess.Persistence --startup-project src/Web --context WorkflowProcessDbContext`. Gera tabelas `WorkflowProcessStoreEvents` e `WorkflowProcessSnapshots` no schema `workflow_process`.

**Build gate**: `dotnet build Societiza.sln` — zero errors antes de avançar.

---

### Fase 5 — Infrastructure

**Referencia**: `references/infrastructure-architecture.md`

#### 1. Infrastructure scaffold

- **Camada**: Infrastructure
- **Tipo**: ModuleInstaller + ServiceInstallers + ApplicationService
- **Acao**: criar
- **Caminho**: `src/Modules/WorkflowProcess/WorkflowProcess.Infrastructure/`
- **Depende de**: Fase 3 · #1, #11, Fase 4 · #1
- **Spec-ref**: pattern de arquitetura
- **Detalhes**: Replicar o padrao do WorkflowTemplate:
  - `WorkflowProcessModuleInstaller.cs` — `IModuleInstaller`, InstallServicesFromAssemblies + Scrutor scan (Infrastructure + Persistence assemblies)
  - `ServiceInstallers/ApplicationServiceInstaller.cs` — TryAddTransient<IWorkflowProcessApplicationService>, AddEventInteractors(), MediatR, FluentValidation, AutoMapper
  - `ServiceInstallers/InfrastructureServiceInstaller.cs` — TryAddTransient<IEventBus, EventBus>
  - `ServiceInstallers/PersistenceServiceInstaller.cs` — AddScoped open generics (IWorkflowProcessProjection<>, IEventStore<WorkflowProcessDbContext>, IUnitOfWork<WorkflowProcessDbContext>), MongoDB context factory, AddDbContext<WorkflowProcessDbContext> com Npgsql
  - `Services/WorkflowProcessApplicationService.cs` — wrapper vazio herdando ApplicationService<WorkflowProcessDbContext>

**Build gate**: `dotnet build Societiza.sln` — zero errors antes de avançar.

---

### Fase 6 — Web

**Referencia**: `references/web-endpoints.md` + `references/web-swagger.md`

#### 1. WorkflowTemplateDataService — implementacao cross-module

- **Camada**: Web
- **Tipo**: Service
- **Acao**: criar
- **Caminho**: `src/Web/Services/CrossModule/WorkflowTemplateDataService.cs`
- **Depende de**: Fase 3 · #2
- **Spec-ref**: §8 "Versionamento do Process — fotografia", "Materializacao por tipo"
- **Detalhes**: Implementacao concreta de `IWorkflowTemplateDataService`. Vive na Web layer pois e o unico lugar onde e permitido conectar multiplos modulos.
  - **Nao acessa MongoDB diretamente.** Injeta `ISender` (MediatR) e despacha `GetWorkflowTemplateDetailByIdQuery`.
  - Fluxo de `GetActiveTemplateSnapshotAsync(templateId, processType, ct)`:
    1. `var result = await _sender.Send(new GetWorkflowTemplateDetailByIdQuery(templateId), ct)`
    2. Se `result.IsFailure` ou `result.Value.Status != TemplateStatus.Active.Name` → retorna `null`
    3. Filtra steps: inclui se `step.AppliesTo.Count == 0 || step.AppliesTo.Contains(processType)`
    4. Mapeia `WorkflowTemplateDetailResponse` → `List<TemplateStepSnapshot>`
    5. Retorna `(result.Value.Version, snapshots)`
  - Referencia apenas `WorkflowTemplate.Shared` e `WorkflowProcess.Shared` — sem referencia a `WorkflowTemplate.Persistence` ou `WorkflowTemplate.Domain`.

#### 2. WorkflowProcessRoutes

- **Camada**: Web
- **Tipo**: Routes
- **Acao**: criar
- **Caminho**: `src/Web/Endpoints/Routes/WorkflowProcessRoutes.cs`
- **Depende de**: nenhum
- **Spec-ref**: §8
- **Detalhes**:
  ```csharp
  internal static class WorkflowProcessRoutes
  {
      internal const string BaseUri = "v{version:apiVersion}/workflow-process";
      internal const string ProcessId = "processId";
      internal const string StepInstanceId = "stepInstanceId";
      internal const string TaskInstanceId = "taskInstanceId";
      internal const string FieldInstanceId = "fieldInstanceId";

      internal const string Create = BaseUri;
      internal const string Board = $"{BaseUri}/board";
      internal const string Detail = $"{BaseUri}/{{{ProcessId}:guid}}";
      internal const string CompleteTask = $"{BaseUri}/{{{ProcessId}:guid}}/steps/{{{StepInstanceId}:guid}}/tasks/{{{TaskInstanceId}:guid}}/complete";
      internal const string SkipTask = $"{BaseUri}/{{{ProcessId}:guid}}/steps/{{{StepInstanceId}:guid}}/tasks/{{{TaskInstanceId}:guid}}/skip";
      internal const string FillStepField = $"{BaseUri}/{{{ProcessId}:guid}}/steps/{{{StepInstanceId}:guid}}/fields/{{{FieldInstanceId}:guid}}";
      internal const string FillTaskField = $"{BaseUri}/{{{ProcessId}:guid}}/steps/{{{StepInstanceId}:guid}}/tasks/{{{TaskInstanceId}:guid}}/fields/{{{FieldInstanceId}:guid}}";
  }
  ```

#### 3. Write Endpoints + Requests

- **Camada**: Web
- **Tipo**: Endpoint
- **Acao**: criar
- **Caminho**: `src/Web/Endpoints/Modules/WorkflowProcess/`
- **Depende de**: #2, Fase 2 · #2, #3, #4, #5, #6
- **Spec-ref**: §8
- **Detalhes**: 5 endpoints de escrita:
  - **CreateWorkflowProcessEndpoint** (POST) — Request: `{ TemplateId, ProcessType, TargetClient, AccountancyId }`. Mapeia para `CreateWorkflowProcessCommand`. **Nao injeta projections do WorkflowTemplate, nao monta snapshots** — toda essa logica esta no handler via `IWorkflowTemplateDataService`. Resultado: `NotFound` (template nao ativo) ou `Ok` com `IdentifierResponse`.
  - **CompleteProcessTaskEndpoint** (POST) — `{ ProcessId, StepInstanceId, TaskInstanceId }` via [FromRoute].
  - **SkipProcessTaskEndpoint** (POST) — `{ ProcessId, StepInstanceId, TaskInstanceId }` via [FromRoute].
  - **FillProcessStepFieldEndpoint** (PUT) — `{ ProcessId, StepInstanceId, FieldInstanceId }` via [FromRoute] + `{ Value }` via body.
  - **FillProcessTaskFieldEndpoint** (PUT) — `{ ProcessId, StepInstanceId, TaskInstanceId, FieldInstanceId }` via [FromRoute] + `{ Value }` via body.
  - Todos seguem pipeline railway: Result.Create → Map → Bind → Match

#### 4. Read Endpoints + Requests

- **Camada**: Web
- **Tipo**: Endpoint
- **Acao**: criar
- **Caminho**: `src/Web/Endpoints/Modules/WorkflowProcess/`
- **Depende de**: #2, Fase 2 · #7, #8
- **Spec-ref**: §1 "board operacional", §8 "Filtragem por tipo"
- **Detalhes**: 2 endpoints de leitura:
  - **GetProcessBoardEndpoint** (GET) — `{ AccountancyId, ProcessType? }` via [FromQuery]. Retorna `List<ProcessBoardItemResponse>`.
  - **GetProcessDetailEndpoint** (GET) — `{ ProcessId }` via [FromRoute]. Retorna `ProcessDetailResponse` com arvore completa.

#### 5. MassTransit Consumers

- **Camada**: Web
- **Tipo**: Consumer
- **Acao**: criar
- **Caminho**: `src/Web/ServiceInstallers/EventBus/Consumers/WorkflowProcessModule/`
- **Depende de**: Fase 1 · #6, Fase 3 · #10
- **Spec-ref**: pattern de arquitetura
- **Detalhes**: 5 consumer classes, um por projection handler:
  - `ProjectionWorkflowProcessConsumer` — IConsumer de WorkflowProcessCreated, WorkflowProcessCompleted
  - `ProjectionProcessStepInstanceConsumer` — IConsumer de ProcessStepInstanceMaterialized, ProcessStepInstanceStarted, ProcessStepInstanceCompleted
  - `ProjectionProcessStepTaskInstanceConsumer` — IConsumer de ProcessStepTaskInstanceMaterialized, ProcessStepTaskInstanceCompleted, ProcessStepTaskInstanceSkipped
  - `ProjectionProcessStepFieldInstanceConsumer` — IConsumer de ProcessStepFieldInstanceMaterialized, ProcessStepFieldInstanceFilled
  - `ProjectionProcessStepTaskFieldInstanceConsumer` — IConsumer de ProcessStepTaskFieldInstanceMaterialized, ProcessStepTaskFieldInstanceFilled
  - Todos delegam imediatamente para o event handler — zero logica

#### 6. ConsumerConfiguration

- **Camada**: Web
- **Tipo**: ConsumerRegistration
- **Acao**: criar
- **Caminho**: `src/Web/ServiceInstallers/EventBus/Consumers/WorkflowProcessModule/ConsumerConfiguration.cs`
- **Depende de**: #5
- **Spec-ref**: pattern de arquitetura
- **Detalhes**: `IEventReceiveEndpointConfiguration` com MODULE_NAME `"workflow-process"`. Um `ConfigureEventReceiveEndpoint` por par <Consumer, Event> — total de 12 registros.

#### 7. CrossModuleServicesInstaller

- **Camada**: Web
- **Tipo**: ServiceInstaller
- **Acao**: criar
- **Caminho**: `src/Web/ServiceInstallers/CrossModuleServicesInstaller.cs`
- **Depende de**: #1, Fase 3 · #2
- **Spec-ref**: pattern de arquitetura (composition root)
- **Detalhes**: `IServiceInstaller` que registra as implementacoes cross-module:
  ```csharp
  services.AddScoped<IWorkflowTemplateDataService, WorkflowTemplateDataService>();
  ```
  Unico ponto do sistema onde WorkflowProcess.Application e WorkflowTemplate estao conectados — via DI, sem referencia direta entre modulos. Descoberto automaticamente pelo `InstallServicesFromAssemblies`.

#### 8. Registration updates

- **Camada**: Web
- **Tipo**: Registration
- **Acao**: modificar
- **Caminho**: `src/Web/`
- **Depende de**: Fase 4 · #1, #3, Fase 5 · #1
- **Spec-ref**: pattern de arquitetura
- **Detalhes**:
  - `StartupTasks/MigrateDatabaseStartupTask.cs` — adicionar `await MigrateDatabaseAsync<WorkflowProcessDbContext>(scope, stoppingToken)`
  - `Web.csproj` — adicionar referencia a `WorkflowProcess.Infrastructure` e `WorkflowProcess.Shared`

**Build gate**: `dotnet build Societiza.sln` — zero errors. Depois: `dotnet test Societiza.sln` — todos os testes passando.

---

## Checklist de registro

- [x] `ConsumerConfiguration.cs` — criar em `WorkflowProcessModule/` com 12 `ConfigureEventReceiveEndpoint` (Fase 6 · #6)
- [x] `AddEventInteractors()` — criar `ServiceCollectionExtensions.cs` em WorkflowProcess.Application com 5 projection handlers (Fase 3 · #11)
- [x] `MigrateDatabaseStartupTask` — adicionar `WorkflowProcessDbContext` (Fase 6 · #8)
- [ ] `RegisterRecurringJobsStartupTask` — nenhum job recorrente nesta feature
- [x] `PersistenceServiceInstaller` — criar com open generics: `IWorkflowProcessProjection<>`, `IEventStore<WorkflowProcessDbContext>`, `IUnitOfWork<WorkflowProcessDbContext>` (Fase 5 · #1)
- [x] `CrossModuleServicesInstaller` — registrar `IWorkflowTemplateDataService → WorkflowTemplateDataService` (Fase 6 · #7)
- [x] `Web.csproj` — adicionar referencia a `WorkflowProcess.Infrastructure` e `WorkflowProcess.Shared` (Fase 6 · #8)

---

## Riscos e pontos de atencao

1. **~~`appliesTo` nao existe no template~~** — **Resolvido.** `WorkflowTemplateStep.AppliesTo` (`List<string>`) foi adicionado ao template. `ProcessStepInstance.IsRequired` foi removido — tudo materializado e obrigatorio.

2. **`IsRequired` nao existe em campos do template** — **Decisao MVP: todos os campos materializam com `IsRequired = false`.** Quando o template for atualizado, o snapshot e o handler podem mapear o valor.

3. **Desacoplamento cross-module via interface anti-corrupcao** — `CreateWorkflowProcessEndpoint` e simples: apenas envia o command. A responsabilidade de buscar e transformar os dados do WorkflowTemplate esta em `IWorkflowTemplateDataService` (Fase 3 · #2), cuja implementacao (Fase 6 · #1) vive na Web layer. A implementacao nao acessa MongoDB diretamente: usa `ISender` para dispatchar `GetWorkflowTemplateDetailByIdQuery`. A conexao e feita exclusivamente no container DI via `CrossModuleServicesInstaller` (Fase 6 · #7). Este padrao (interface no consumidor + MediatR + composition root) sera o modelo para futuras integracoes cross-module no Societiza.

4. **Volume de eventos na criacao** — um template com 10 steps, 5 tasks por step e 3 fields por task/step gera ~241 eventos numa unica operacao de `AppendEventsAsync`. Aceitavel no event store (uma transacao PostgreSQL) mas gera 241 mensagens no RabbitMQ. Monitorar performance. Se necessario, considerar batch publishing.

5. **Processos concluidos nunca sao apagados** — retencao permanente conforme spec §8, §6. O board query deve filtrar adequadamente. Para o MVP, a coluna "concluido" mostra todos.

6. **Avanço automatico em cascata** — `CompleteTask`/`SkipTask` no aggregate pode levantar ate 4 eventos em cascata (task completed → step started → step completed → process completed). O `When()` de cada evento atualiza o estado antes do proximo check. Testar cuidadosamente o cenario de cascata completa.
