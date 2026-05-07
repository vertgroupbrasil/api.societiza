# Integration Contract — WorkflowProcess (Board operacional do societário)

## 1. Identificação da feature

- **Nome da feature**: WorkflowProcess — Board operacional do societário
- **Módulo backend responsável**: `WorkflowProcess` (Domain · Application · Persistence · Infrastructure · Shared)
- **Status atual**: `existente`
- **Data da última atualização**: 2026-05-06
- **Responsável técnico**: Gabriel Carvalho (tech-lead — backend)

A feature está implementada como módulo independente em `src/Modules/WorkflowProcess/`, com endpoints em `src/Web/Endpoints/Modules/WorkflowProcess/` e roteamento canônico em `src/Web/Endpoints/Routes/WorkflowProcessRoutes.cs`. O módulo é event-sourced (escrita) com projeções MongoDB (leitura), seguindo o padrão CQRS dos demais módulos. Migration inicial aplicada em `20260506082147_InitialMigrate`. Cross-module é resolvido via `IWorkflowTemplateDataService`, declarada em `WorkflowProcess.Application` e implementada na Web layer em `Web.Services.CrossModule.WorkflowTemplateDataService`.

---

## 2. Objetivo de negócio da feature

WorkflowProcess é a camada de execução real do societário — onde o contador opera o dia a dia. É um kanban único por contabilidade, em que cada card representa um processo societário em andamento (Abertura, Alteração ou Baixa). O board substitui planilhas, conversas no WhatsApp e memória humana por um único lugar onde o estado de cada processo é visível e rastreável.

A feature existe para fechar o ciclo do societário no produto. A camada de template (`WorkflowTemplate`) já existia, mas sem a camada de execução o contador não tinha onde operar os processos. WorkflowProcess é o elo que transforma a estrutura do workflow em operação real, e, sem ela, o produto não é utilizável para a operação real da contabilidade.

O beneficiário direto é o contador (AccountingAdmin ou AccountingEmployee) — qualquer membro da contabilidade ativa pode criar, abrir, executar tarefas e concluir processos. O beneficiário indireto é a contabilidade como organização, que ganha visibilidade compartilhada da operação. O contexto de uso é a rotina operacional diária: criar processos quando um cliente entra, avançar etapas conforme tarefas são executadas, consultar o board para responder "como está o processo do cliente X?".

A feature conecta-se com `WorkflowTemplate` (origem da estrutura, fotografada como snapshot na criação), `Accountancy` (escopo de visibilidade) e `Identity` (autenticação e Accountancy ativa). É a fonte futura para dashboards (P2), notificações (P3), portal do cliente e versionamento de templates (`societario-template-versioning`).

---

## 3. Escopo atual versus escopo futuro

**O que já está implementado no backend hoje:**

- Criação de processo a partir de um `TemplateId` Active, com materialização completa de steps, tasks, fields e task fields como instâncias (snapshot).
- Captura do `TemplateVersion` como string no momento da criação (fotografia imutável).
- Três tipos de processo (`ProcessType`): `Abertura`, `Alteracao`, `Baixa`. Filtragem opcional por tipo no board.
- Board operacional: lista de processos da contabilidade ativa com `CurrentStepTitle`, `TotalSteps`, `CompletedSteps`, ordenado por `CreatedAt` desc. **Importante: o board atual retorna processos `InProgress` E `Completed`** — o filtro por status `InProgress` previsto na spec não foi aplicado na query (ver gap em §16).
- Detalhe completo do processo: steps (com Order, Status, Tasks e Fields), tasks (Status, IsOptional, CompletedAt, SkippedAt e Fields), fields (Label, FieldType, Options, Order, Value, FilledAt) — tudo ordenado por `Order`.
- Completar tarefa (`CompleteProcessTask`) com pré-condições: processo InProgress; step deve ser o atual (primeiro não-Completed); task Pending; task obrigatória anterior resolvida.
- Pular tarefa (`SkipProcessTask`) com as mesmas pré-condições. **Atenção: o backend NÃO valida explicitamente `IsOptional = true` no skip** — qualquer task Pending pode ser pulada (ver gap em §16).
- Reverter tarefa (`RevertProcessTask`): só permitido se task estiver Completed/Skipped e nenhum step com Order superior tiver progresso (Status diferente de NotStarted).
- Preencher campo de step (`FillProcessStepField`) e de task (`FillProcessTaskField`) — **com pré-condição de "step atual"** (ver gap em §16: a spec/logic indicam que campos podem ser preenchidos a qualquer momento, sem essa restrição).
- Auto-conclusão de step quando todas as required tasks (`IsOptional = false`) estão resolvidas (Completed ou Skipped).
- Auto-conclusão do processo quando todos os steps estão Completed; publica domain event `WorkflowProcessCompleted`.
- Reversão em cadeia: ao reverter uma task, se o step estava Completed volta para InProgress; se o processo estava Completed publica `WorkflowProcessReverted` e volta para InProgress.
- Persistência event-sourced em PostgreSQL (`workflow_process.events` + `workflow_process.snapshots`).
- Projeções MongoDB para `WorkflowProcess`, `ProcessStepInstance`, `ProcessStepTaskInstance`, `ProcessStepFieldInstance`, `ProcessStepTaskFieldInstance`, atualizadas via event handlers do MassTransit.
- Autorização: todas as rotas exigem `[Authorize]`. Regra: `currentUser.Role != SystemAdmin && currentUser.AccountancyId != process.AccountancyId` → `Forbidden`. SystemAdmin atravessa contabilidades.

**O que ainda não está implementado:**

- Vista/histórico de processos `Completed` separada do board operacional (feature `societario-process-history`, conforme spec §6).
- Notificações, alertas de gargalo ou e-mails atrelados a transições.
- Dashboard agregado de métricas.
- Filtros avançados no board (status, período, etc.) — apenas filtro por `processType` está exposto.
- Acesso do cliente final (portal) ao processo.

**O que existe no domínio mas não está exposto por endpoint:**

- Projeções `ProcessStepFieldInstance` e `ProcessStepTaskFieldInstance` são consumidas apenas via `GetProcessDetail`. Não há endpoints granulares para listar/consultar campos individualmente — sempre via detalhe completo.
- O domain event `WorkflowProcessCompleted` é publicado via EventBus (MassTransit/RabbitMQ) e consumido pela própria projeção; ainda não há consumers em outros módulos (versionamento etc.).
- Auditoria (timestamps `CompletedAt`, `SkippedAt`, `FilledAt`) é registrada, mas o `userId` do executor não é capturado em nenhum estado (decisão de produto §6 da spec).

**O que é apenas visão futura de produto:**

- Personalização de templates pela contabilidade (a estrutura de `TemplateId` explícito na criação prepara terreno).
- Tipos de processo configuráveis pela contabilidade.
- Múltiplos templates por contabilidade.
- Importação de processos legados.

**O que ainda está em discussão:**

- Nada bloqueante no backend. Pendências documentadas como gaps em §16.

---

## 4. Contexto de produto e semântica da feature

O usuário percebe a feature como o "lugar onde o trabalho acontece". Abre o board, vê um kanban com cards por processo, sabe imediatamente em que etapa cada um está. Cria processos novos escolhendo template + tipo + cliente-alvo (texto livre). Avançar não é uma ação — é consequência: ao concluir as tarefas obrigatórias da etapa atual, o step fecha sozinho e o próximo abre automaticamente.

Sucesso = card sai do board (processo concluído automaticamente após todos os steps terem fechado). Erro funcional = ação rejeitada com mensagem clara (step bloqueado, task anterior pendente, task já resolvida, processo já concluído, downstream com progresso impede revert).

Estados que importam:

- **Processo:** InProgress / Completed (terminal).
- **Step:** NotStarted / InProgress / Completed.
- **Task:** Pending / Completed / Skipped. (Não há estado `Reverted` persistido — uma task revertida volta para `Pending`.)
- **Campo:** "sem valor" (Value null) / "com valor" (Value preenchido + FilledAt).

Transições críticas para o frontend:

- Completar/pular uma task pode auto-concluir o step e o processo no mesmo ciclo. **O frontend deve recarregar o estado completo do processo após cada mutation** — é impossível prever localmente quantas transições uma única ação dispara.
- Reverter uma task pode reabrir o step (Completed → InProgress) e até o processo (Completed → InProgress, com publicação de `WorkflowProcessReverted`).
- Após o processo concluir, qualquer mutação retorna `409 Conflict — WorkflowProcess.AlreadyCompleted` (exceto `RevertProcessTask`, que é justamente o caminho para "destravar" um processo concluído por engano).

---

## 5. Dependências e relações com outras features

**Depende de:**

- **`WorkflowTemplate`** — fonte da estrutura. A criação chama `IWorkflowTemplateDataService.GetActiveTemplateSnapshotAsync(templateId, ct)` (implementação Web em `Web.Services.CrossModule.WorkflowTemplateDataService`), que envia internamente `GetWorkflowTemplateDetailByIdQuery` e retorna `null` se status ≠ `Active`. O processo só é criado se o template for retornado.
- **`Accountancy`** — todo processo pertence a um `AccountancyId`. O board é escopado pela Accountancy ativa.
- **`Identity`** — `[Authorize]` em todas as rotas. `ICurrentUserService` fornece `Role` e `AccountancyId`. SystemAdmin pode atuar em qualquer contabilidade; demais perfis só na sua.

**Consumida por (futuro):** dashboards, notificações, histórico (`societario-process-history`), portal do cliente, módulo de versionamento de templates.

**Posição no produto:** feature core do MVP. Sem ela, o produto não é utilizável para operação real.

---

## 6. Rotas públicas

Base path completo: `/api/v1/workflow-process` (UsePathBase + versionamento).

| Método | Path | Tipo | Finalidade | Params de rota | Pronta para frontend? |
|---|---|---|---|---|---|
| POST | `/api/v1/workflow-process` | Command | Criar processo a partir de template Active | — | Sim |
| GET | `/api/v1/workflow-process` | Query | Listar processos para o board | — (query: `accountancyId`, `processType?`) | Sim, com gap (§16: retorna Completed também) |
| GET | `/api/v1/workflow-process/{processId}` | Query | Detalhe completo do processo | `processId` | Sim |
| POST | `/api/v1/workflow-process/{processId}/steps/{stepInstanceId}/tasks/{taskInstanceId}/complete` | Command | Concluir tarefa | `processId`, `stepInstanceId`, `taskInstanceId` | Sim |
| POST | `/api/v1/workflow-process/{processId}/steps/{stepInstanceId}/tasks/{taskInstanceId}/skip` | Command | Pular tarefa | idem | Sim, com gap (§16: não valida `IsOptional`) |
| POST | `/api/v1/workflow-process/{processId}/steps/{stepInstanceId}/tasks/{taskInstanceId}/revert` | Command | Reverter tarefa | idem | Sim |
| PUT | `/api/v1/workflow-process/{processId}/steps/{stepInstanceId}/fields/{fieldInstanceId}` | Command | Preencher campo de step | `processId`, `stepInstanceId`, `fieldInstanceId` | Sim, com gap (§16: exige step atual) |
| PUT | `/api/v1/workflow-process/{processId}/steps/{stepInstanceId}/tasks/{taskInstanceId}/fields/{fieldInstanceId}` | Command | Preencher campo de task | idem + `taskInstanceId` | Sim, com gap (§16: exige step atual) |

Constantes de rota canônicas: `src/Web/Endpoints/Routes/WorkflowProcessRoutes.cs`.

Tags Swagger:
- `Workflow Process / 01. Processes` — Create, Board, Detail
- `Workflow Process / 02. Tasks` — Complete, Skip, Revert
- `Workflow Process / 03. Fields` — FillStepField, FillTaskField

---

## 7. Contrato de request

### Request — CreateWorkflowProcess (`POST /api/v1/workflow-process`)

Corpo (JSON):
- `accountancyId`: `Guid`, obrigatório, ≠ `Guid.Empty` — contabilidade dona do processo.
- `templateId`: `Guid`, obrigatório, ≠ `Guid.Empty` — template a ser usado (deve estar Active).
- `processType`: `string`, obrigatório, valores aceitos `Abertura` | `Alteracao` | `Baixa` (sem cedilha em "Alteracao", validação em `ProcessType.TryFromName`).
- `targetClient`: `string`, obrigatório, não vazio — texto livre para identificar o cliente-alvo (não é entidade).

Transformações obrigatórias no frontend:
- Enviar `processType` exatamente como o enum (atenção a `Alteracao` sem cedilha).
- `targetClient` é texto livre — frontend não deve tentar formatar como CNPJ ou ID.

Validações relevantes:
- `AccountancyId is required` / `AccountancyId must be a valid GUID`
- `TemplateId is required` / `TemplateId must be a valid GUID`
- `ProcessType is required` / `ProcessType is not valid`
- `TargetClient is required`
- Pós-validação (no handler): se template não existe ou não está Active → `404 Not Found` (`WorkflowProcess.TemplateNotActiveOrNotFound`).
- Autorização: se `currentUser.Role != SystemAdmin && currentUser.AccountancyId != request.AccountancyId` → `403 Forbidden` (`WorkflowProcess.Forbidden`).

---

### Request — GetProcessBoard (`GET /api/v1/workflow-process?accountancyId={...}&processType={...?}`)

Query string:
- `accountancyId`: `Guid`, obrigatório.
- `processType`: `string?`, opcional — quando informado, filtra por `Abertura` | `Alteracao` | `Baixa`.

Validações relevantes:
- Se `currentUser.Role != SystemAdmin && currentUser.AccountancyId != request.AccountancyId` → `403 Forbidden`.
- O backend **não valida** o nome do `processType` nem o `accountancyId` via FluentValidation neste endpoint — se inválido, simplesmente retorna lista vazia ou 403.

---

### Request — GetProcessDetail (`GET /api/v1/workflow-process/{processId}`)

Route:
- `processId`: `Guid`, obrigatório.

Validações relevantes: nenhuma na camada de validador. Se processo não existe → `404 Not Found`. Se contabilidade do processo difere da do usuário (e role não é SystemAdmin) → `403 Forbidden`.

---

### Request — CompleteProcessTask / SkipProcessTask / RevertProcessTask

Route:
- `processId`: `Guid`
- `stepInstanceId`: `Guid`
- `taskInstanceId`: `Guid`

Body: vazio.

Validações de domínio (no handler) listadas em §10.

---

### Request — FillProcessStepField (`PUT .../steps/{stepInstanceId}/fields/{fieldInstanceId}`)

Route:
- `processId`: `Guid`
- `stepInstanceId`: `Guid`
- `fieldInstanceId`: `Guid`

Body (JSON):
- `value`: `string`, obrigatório, não vazio.

Transformações no frontend:
- `Value` é sempre string. Para tipos `Boolean`, `Integer`, `Date`, `Select`, o frontend é responsável por serializar (ex.: `"true"`, `"42"`, `"2026-05-06"`, valor textual da opção).
- O backend **não valida o conteúdo** do `Value` por `FieldType` — qualquer string passa.
- `Value is required` (FluentValidation) — string vazia é rejeitada.

---

### Request — FillProcessTaskField (`PUT .../tasks/{taskInstanceId}/fields/{fieldInstanceId}`)

Mesmo formato de `FillProcessStepField`, com `taskInstanceId` adicional na rota.

---

## 8. Contrato de response

### Response — CreateWorkflowProcess

`200 OK` — `IdentifierResponse`:
- `id`: `Guid` — identificador do processo recém-criado. Use para navegar para o detalhe.

Após sucesso, o frontend deve invalidar o cache do board (lista) para refletir o novo processo. O processo nasce em `Status = InProgress` e o primeiro step em `InProgress` (transição publicada via `ProcessStepInstanceStarted` quando uma task é executada — observação: na criação as steps nascem `NotStarted` no domain; só vão para `InProgress` quando uma task daquele step é tocada — ver gap em §16).

---

### Response — GetProcessBoard

`200 OK` — `List<ProcessBoardItemResponse>`:

Cada item:
- `id`: `Guid` | identificador estável do processo | usar como chave de reconciliação.
- `processType`: `string` | `Abertura` | `Alteracao` | `Baixa` | renderizar etiqueta no card.
- `targetClient`: `string` | texto livre informado pelo contador | exibir como título do card.
- `status`: `string` | `InProgress` | `Completed` | filtrar/destacar; **atualmente o board retorna ambos** (gap §16).
- `currentStepTitle`: `string?` | título do primeiro step ainda não Completed | `null` se todos os steps estiverem Completed (processo concluído) — usar para indicar "etapa atual".
- `totalSteps`: `int` | quantidade total de steps materializados | usar para barra de progresso.
- `completedSteps`: `int` | quantidade de steps com status `Completed` | usar para barra de progresso.
- `createdAt`: `DateTimeOffset` (ISO-8601 com offset) | quando o processo foi criado | ordenação atual.
- `completedAt`: `DateTimeOffset?` | preenchido apenas quando o processo está Completed | exibir como data de conclusão.

### 8.1. Formato canônico versus formato de exibição

- Datas (`createdAt`, `completedAt`, `completedAt` de step e task, `skippedAt`, `filledAt`): `DateTimeOffset` ISO-8601 com offset UTC. Frontend formata para fuso local.
- Enums (`processType`, `status`, `fieldType`): nomes canônicos sem máscara. Frontend traduz para PT-BR de exibição.
- `targetClient`: livre — sem máscara.
- `value` de campo: sempre string, em formato canônico decidido pelo cliente (não há transformação no backend).

### 8.2. Nullabilidade e ausência de campo

- `currentStepTitle` é `null` quando todos os steps estão Completed (cenário possível no board enquanto `Completed` não for filtrado fora — gap §16).
- `completedAt` (process) é `null` enquanto `status = InProgress`; preenchido na transição.
- `completedAt` (step) é `null` enquanto step não chegou a `Completed`; é zerado em revert.
- `completedAt` / `skippedAt` (task) são mutuamente exclusivos:
  - Task `Completed` → `completedAt` preenchido, `skippedAt` null.
  - Task `Skipped` → `skippedAt` preenchido, `completedAt` null.
  - Task `Pending` (incluindo após revert) → ambos null.
- `value` / `filledAt` (campo) são `null` enquanto o campo não foi preenchido.
- `options` (campo) é `null` para FieldTypes que não usam options (ex.: `Text`, `Boolean`, `Integer`, `Date`); preenchido como string para `Select` (formato definido pelo template, opaco para o backend).

---

### Response — GetProcessDetail

`200 OK` — `ProcessDetailResponse`:

- `id`: `Guid` — id estável do processo.
- `accountancyId`: `Guid` — para conferência local de escopo.
- `templateId`: `Guid` — referência ao template-fonte.
- `templateVersion`: `string` — fotografia da versão do template no momento da criação. **String — não é número, não é semver garantido. É opaco.**
- `processType`: `string` — `Abertura` | `Alteracao` | `Baixa`.
- `targetClient`: `string` — texto livre.
- `status`: `string` — `InProgress` | `Completed`.
- `createdAt`: `DateTimeOffset`.
- `completedAt`: `DateTimeOffset?`.
- `steps`: `List<ProcessStepInstanceResponse>` — sempre presente, ordenado por `Order` ascendente.

`ProcessStepInstanceResponse`:
- `id`: `Guid`
- `title`: `string`
- `order`: `int` — ordem de execução; **chave de ordenação canônica**, frontend não deve reordenar.
- `status`: `string` — `NotStarted` | `InProgress` | `Completed`.
- `completedAt`: `DateTimeOffset?`
- `tasks`: `List<ProcessStepTaskInstanceResponse>` — ordenado por `Order`.
- `fields`: `List<ProcessStepFieldInstanceResponse>` — ordenado por `Order`.

`ProcessStepTaskInstanceResponse`:
- `id`: `Guid`
- `title`: `string`
- `order`: `int`
- `isOptional`: `bool` — usar para habilitar/desabilitar ação de skip na UI.
- `status`: `string` — `Pending` | `Completed` | `Skipped`.
- `completedAt`: `DateTimeOffset?`
- `skippedAt`: `DateTimeOffset?`
- `fields`: `List<ProcessStepTaskFieldInstanceResponse>` — ordenado por `Order`.

`ProcessStepFieldInstanceResponse` / `ProcessStepTaskFieldInstanceResponse`:
- `id`: `Guid`
- `label`: `string`
- `fieldType`: `string` — `Text` | `Boolean` | `Integer` | `Select` | `Date` (definidos pelo template; backend não os valida, apenas repassa).
- `options`: `string?` — payload opaco (esp. para `Select`).
- `order`: `int`
- `value`: `string?` — `null` enquanto não preenchido.
- `filledAt`: `DateTimeOffset?`

---

### Response — CompleteProcessTask / SkipProcessTask / RevertProcessTask / FillProcessStepField / FillProcessTaskField

`200 OK` — sem corpo. Frontend deve recarregar `GetProcessDetail` (e, se aplicável, `GetProcessBoard`) para ver o estado final.

---

## 9. Estados de negócio e transições

### Processo (`ProcessStatus`)

- `InProgress` (0) → estado inicial obrigatório na criação.
- `Completed` (1) → terminal; alcançado automaticamente quando todos os steps estão Completed. Publica domain event `WorkflowProcessCompleted`.
- `Completed → InProgress`: pode acontecer via `RevertProcessTask` quando a reversão derruba algum step de Completed para InProgress. Publica `WorkflowProcessReverted` e zera `CompletedAt`.

### Step (`StepInstanceStatus`)

- `NotStarted` (0): estado de criação.
- `InProgress` (1): atingido **quando uma task daquele step é completada/pulada e o step estava `NotStarted`** (a transição é disparada dentro de `CompleteTask`/`SkipTask` no aggregate). **Observação importante:** na implementação atual, ao criar o processo as steps nascem todas `NotStarted` — inclusive a primeira. O primeiro step só vai para `InProgress` no momento da primeira ação (complete/skip) sobre uma de suas tasks. (Ver gap §16: a spec diz "primeira step fica InProgress imediatamente").
- `Completed` (2): atingido automaticamente quando todas as required tasks (`IsOptional = false`) estão resolvidas (Completed ou Skipped).
- `Completed → InProgress` (ou eventualmente `NotStarted` se todas as tasks ficaram Pending): via revert.

### Task (`TaskInstanceStatus`)

- `Pending` (0): estado inicial e estado pós-revert.
- `Completed` (1): via `CompleteProcessTask`.
- `Skipped` (2): via `SkipProcessTask`.
- Não há estado persistido `Reverted`. Revert volta para `Pending` (zera `CompletedAt` e `SkippedAt`). O event `ProcessStepTaskInstanceReverted` é publicado para fins de event sourcing/auditoria.

### Campo (`ProcessStepFieldInstance` / `ProcessStepTaskFieldInstance`)

- "Sem valor" (`Value = null`, `FilledAt = null`) → "Com valor" (`Value`, `FilledAt` setados). Re-preenchimento sobrescreve.

### 9.1. Ordenação, filtros e agrupamentos com semântica de negócio

- **Board (`GetProcessBoard`)**: ordenado por `CreatedAt` desc. Frontend deve **preservar** essa ordem.
- **Detail (`GetProcessDetail`)**: steps por `Order` asc; tasks por `Order` asc; fields por `Order` asc — ordem é canônica (ordem de execução do template). Frontend nunca deve reordenar.
- Filtro implícito do board: `accountancyId == AccountancyId && IsDeleted == false`. **Não há filtro por `Status = InProgress`** — gap §16.
- Filtro opcional: `processType` quando informado.
- Não há paginação atualmente — board retorna todos os processos da contabilidade.

---

## 10. Erros esperados e edge cases

Envelope canônico de erro (Core): `{ statusCode, message, path, traceId, errors, detail }`.

Catálogo (`WorkflowProcessErrors`):

| Código | HTTP | Quando acontece | UX sugerida |
|---|---|---|---|
| `WorkflowProcess.Forbidden` | 403 | Usuário não-SystemAdmin tenta operar processo de outra contabilidade | Mensagem genérica de acesso negado |
| `WorkflowProcess.TemplateNotActiveOrNotFound` | 404 | TemplateId inexistente ou status ≠ Active no momento da criação | Frontend deve oferecer apenas templates Active e tratar como condição de corrida |
| `WorkflowProcess.NotFound` | 404 | ProcessId não existe (ou não há aggregate carregável) | "Processo não encontrado." Recarregar board. |
| `WorkflowProcess.AlreadyCompleted` | 409 | Mutação em processo Completed (exceto revert) | Bloquear ações no card, oferecer "histórico" |
| `WorkflowProcess.StepNotFound` | 404 | StepInstanceId não pertence ao processo | Recarregar detalhe |
| `WorkflowProcess.TaskNotFound` | 404 | TaskInstanceId não pertence ao step | Recarregar detalhe |
| `WorkflowProcess.TaskAlreadyResolved` | 409 | Complete/Skip em task que já está Completed ou Skipped | Recarregar detalhe |
| `WorkflowProcess.StepFieldNotFound` | 404 | FieldInstanceId não pertence ao step | Recarregar detalhe |
| `WorkflowProcess.TaskFieldNotFound` | 404 | FieldInstanceId não pertence à task | Recarregar detalhe |
| `WorkflowProcess.StepNotUnlocked` | 409 | Ação contra step que não é o atual (existe step anterior não-Completed) | "Conclua os passos anteriores antes de continuar." |
| `WorkflowProcess.TaskNotRevokable` | 409 | Revert em task que está `Pending` | Esconder ação de revert para tasks Pending |
| `WorkflowProcess.PreviousRequiredTaskNotResolved` | 409 | Complete/Skip antes de resolver task obrigatória anterior; mensagem cita o `title` da task bloqueante | Exibir mensagem do backend literal — ela é contextualizada |
| `WorkflowProcess.DownstreamStepHasProgress` | 409 | Revert quando algum step downstream (Order maior) tem `Status != NotStarted`; mensagem cita o `title` do step bloqueante | Exibir mensagem do backend literal |

Frontend deve:
- Bloquear preventivamente o que dá para bloquear (skip em task obrigatória pela UI; revert em tasks Pending).
- Para os demais 409, mostrar a mensagem do backend (já em inglês — ver gap §16) e oferecer "tentar de novo após recarregar".
- Tratar 403 de forma genérica.

---

## 11. Semântica de autenticação e autorização

- Todas as rotas exigem autenticação — `[Authorize]` em todos os endpoints.
- Contexto assumido: `ICurrentUserService` provê `Role` e `AccountancyId`.
- Regra única de autorização aplicada em todo handler:
  ```
  if (currentUser.Role != IdentityRoles.SystemAdmin
      && currentUser.AccountancyId != process.AccountancyId)
      return Forbidden;
  ```
- Não há diferenciação por papel dentro da contabilidade: `AccountingAdmin` e `AccountingEmployee` têm exatamente as mesmas capacidades operacionais. Conforme decidido na spec §6.
- `SystemAdmin` pode operar processos de qualquer contabilidade.
- Não há gap entre auth real e esperada para esta feature — autenticação JWT já é exigida e a Accountancy ativa do usuário já está disponível no `currentUser`.

---

## 12. Impacto de UX que o frontend precisa saber

- **`CreateWorkflowProcess` retorna id novo** (`IdentifierResponse.id`). Use-o para navegar imediatamente para o detalhe.
- **Board não retorna o processo recém-criado por reconciliação otimista garantida**: a projeção MongoDB é eventual. Após criar, recomenda-se invalidar a query do board e refazer (latência típica baixa, mas não há garantia de read-your-writes).
- **`Complete`/`Skip`/`Revert` são instantâneos no PostgreSQL (event store) mas dependem de propagação MongoDB para o board e detalhe**. Após qualquer mutation, **recarregar o detalhe** é seguro; pode haver pequeno delay no board.
- **Auto-conclusão em cascata**: completar uma task pode disparar conclusão de step **e** conclusão do processo na mesma chamada. Frontend deve recarregar o estado completo, não inferir transições localmente.
- **Revert pode reabrir step e processo**: idem — recarregar.
- **Skip só faz sentido em task `IsOptional = true`** — o frontend deve esconder/desabilitar a ação de skip para `IsOptional = false`. **Atenção: o backend não rejeita skip de task obrigatória atualmente** (gap §16) — confiar apenas na UI gera bug.
- **Campos podem ser preenchidos a qualquer momento (segundo a spec/logic), MAS o backend atual exige que o step seja o "step atual"** — gap §16. Frontend deve esperar `409 WorkflowProcess.StepNotUnlocked` quando tentar preencher um campo de step não-atual e tratar essa mensagem.
- **Re-preenchimento é permitido**: `FillProcessStepField` em campo já preenchido sobrescreve `Value` e `FilledAt`.
- **Não há redirect**: depois de qualquer mutation, o frontend permanece na mesma rota e revalida.
- **Ordenação canônica de steps/tasks/fields é o `Order`** — não reordenar localmente.
- **`targetClient` é texto livre** — sem máscara, sem CNPJ, sem busca por entidade.

---

## 13. Impacto de dados no frontend

| Mutation | Invalida |
|---|---|
| `CreateWorkflowProcess` | board (lista) |
| `CompleteProcessTask` | detail desse `processId`, board |
| `SkipProcessTask` | detail desse `processId`, board |
| `RevertProcessTask` | detail desse `processId`, board |
| `FillProcessStepField` | detail desse `processId` |
| `FillProcessTaskField` | detail desse `processId` |

- O detail é a fonte de verdade após qualquer mutation. Stale data risk é principalmente no board, que reflete projeção MongoDB com latência.
- **UI otimista** — possível para campos (`Value`/`FilledAt`) com baixo risco; arriscada para ações que disparam cascata (Complete/Skip podem auto-concluir step e processo). Recomenda-se otimismo só em "checkbox de campo Boolean" e similares; para complete/skip/revert, esperar o servidor.

### 13.1. Identificadores estáveis e chaves de reconciliação

- Chaves estáveis: `processId`, `stepInstanceId`, `taskInstanceId`, `fieldInstanceId`, `accountancyId`, `templateId`. Todos `Guid` gerados pelo backend, imutáveis.
- `templateVersion` é string e **não muda no processo após criação** (snapshot).
- Não usar `order`, `title`, `targetClient`, `value` como chaves — todos podem mudar (title via revisão futura, targetClient via comportamento futuro, value via re-preenchimento).
- `templateStepId` / `templateTaskId` / `templateFieldId` chegam apenas como referência interna nos eventos — **não são expostos nas responses HTTP**, apenas nos snapshots de domínio.

### 13.2. Compatibilidade e evolução de contrato

- Esta é a **primeira versão** da feature, não substitui contrato anterior.
- A presença explícita de `templateVersion` (string) e a publicação de `WorkflowProcessCompleted` são pontos de extensão para o módulo futuro `societario-template-versioning`.
- Mudanças futuras esperadas (não breaking previstas):
  - Filtro implícito de `Status = InProgress` no board (correção de gap §16).
  - Validação de `IsOptional = true` no skip (correção de gap §16).
  - Remoção da exigência de "step atual" em fill de campo (alinhamento com spec §16).
- Mudanças futuras potencialmente breaking:
  - Histórico (`Completed`) sair do board principal e mudar de endpoint.
  - Paginação no board.

---

## 14. Observabilidade e suporte

- Toda resposta de erro do envelope Core contém `traceId` (correlation id propagado via middleware Core).
- Logs estruturados nos event handlers de projeção (`ILogger<T>`): warning quando projeção não encontrada (lag eventual), error + throw em falha de gravação.
- Recomenda-se que o frontend registre `traceId` em logs de UX (ex.: Sentry) para diagnóstico cruzado.
- Falhas em projeções MongoDB **não bloqueiam a mutation** (commits PostgreSQL já passaram), mas geram exceção que sobe para o consumer MassTransit — filas de retry/DLQ aplicam-se conforme infraestrutura genérica do Core.

---

## 15. Exemplos reais de payload

```json
// Request — CreateWorkflowProcess  POST /api/v1/workflow-process
{
  "accountancyId": "f2e1b9d4-7c3a-4f4e-9e10-8c6b1a2d4e5f",
  "templateId":    "a1b2c3d4-e5f6-4789-a012-3456789abcde",
  "processType":   "Abertura",
  "targetClient":  "Padaria Pão Quente Ltda — sócio João Silva"
}

// Response — CreateWorkflowProcess  200 OK
{
  "id": "11111111-2222-3333-4444-555555555555"
}

// Response — GetProcessBoard  GET /api/v1/workflow-process?accountancyId=f2e1b9d4-7c3a-4f4e-9e10-8c6b1a2d4e5f
[
  {
    "id":              "11111111-2222-3333-4444-555555555555",
    "processType":     "Abertura",
    "targetClient":    "Padaria Pão Quente Ltda — sócio João Silva",
    "status":          "InProgress",
    "currentStepTitle":"Coleta de documentos",
    "totalSteps":      4,
    "completedSteps":  0,
    "createdAt":       "2026-05-06T13:42:11.123+00:00",
    "completedAt":     null
  },
  {
    "id":              "99999999-8888-7777-6666-555555555555",
    "processType":     "Baixa",
    "targetClient":    "Mercearia Antiga ME",
    "status":          "Completed",
    "currentStepTitle": null,
    "totalSteps":      3,
    "completedSteps":  3,
    "createdAt":       "2026-04-29T08:11:00.000+00:00",
    "completedAt":     "2026-05-04T17:22:33.000+00:00"
  }
]

// Response — GetProcessDetail  GET /api/v1/workflow-process/{processId}  200 OK
{
  "id":              "11111111-2222-3333-4444-555555555555",
  "accountancyId":   "f2e1b9d4-7c3a-4f4e-9e10-8c6b1a2d4e5f",
  "templateId":      "a1b2c3d4-e5f6-4789-a012-3456789abcde",
  "templateVersion": "1.0.0",
  "processType":     "Abertura",
  "targetClient":    "Padaria Pão Quente Ltda — sócio João Silva",
  "status":          "InProgress",
  "createdAt":       "2026-05-06T13:42:11.123+00:00",
  "completedAt":     null,
  "steps": [
    {
      "id":          "aaaa1111-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      "title":       "Coleta de documentos",
      "order":       1,
      "status":      "InProgress",
      "completedAt": null,
      "tasks": [
        {
          "id":          "bbbb1111-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
          "title":       "Receber RG e CPF do sócio",
          "order":       1,
          "isOptional":  false,
          "status":      "Completed",
          "completedAt": "2026-05-06T14:05:00.000+00:00",
          "skippedAt":   null,
          "fields": [
            {
              "id":        "cccc1111-cccc-cccc-cccc-cccccccccccc",
              "label":     "Número do RG",
              "fieldType": "Text",
              "options":   null,
              "order":     1,
              "value":     "12.345.678-9",
              "filledAt":  "2026-05-06T14:04:50.000+00:00"
            }
          ]
        },
        {
          "id":          "bbbb2222-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
          "title":       "Anexar comprovante de endereço",
          "order":       2,
          "isOptional":  true,
          "status":      "Pending",
          "completedAt": null,
          "skippedAt":   null,
          "fields": []
        }
      ],
      "fields": [
        {
          "id":        "dddd1111-dddd-dddd-dddd-dddddddddddd",
          "label":     "Tipo de empresa",
          "fieldType": "Select",
          "options":   "[\"MEI\",\"LTDA\",\"SA\"]",
          "order":     1,
          "value":     "LTDA",
          "filledAt":  "2026-05-06T13:43:01.000+00:00"
        }
      ]
    }
  ]
}

// Erro de validação (FluentValidation) — 400 Bad Request
{
  "statusCode": 400,
  "message":    "One or more validation errors occurred.",
  "path":       "/api/v1/workflow-process",
  "traceId":    "00-abcdef0123456789abcdef0123456789-0123456789abcdef-01",
  "errors": {
    "ProcessType":  ["ProcessType is not valid"],
    "TargetClient": ["TargetClient is required"]
  },
  "detail": null
}

// Conflito — 409 (PreviousRequiredTaskNotResolved)
{
  "statusCode": 409,
  "message":    "Task 'Receber RG e CPF do sócio' must be completed before proceeding to the next task.",
  "path":       "/api/v1/workflow-process/11111111-2222-3333-4444-555555555555/steps/aaaa1111.../tasks/bbbb2222.../complete",
  "traceId":    "00-abcdef0123456789abcdef0123456789-0123456789abcdef-01",
  "errors":     null,
  "detail":     "WorkflowProcess.PreviousRequiredTaskNotResolved"
}

// Conflito — 409 (DownstreamStepHasProgress)
{
  "statusCode": 409,
  "message":    "Step 'Registro na Junta Comercial' has progress. Revert it before reverting this step.",
  "path":       "/api/v1/workflow-process/.../tasks/.../revert",
  "traceId":    "00-...-01",
  "errors":     null,
  "detail":     "WorkflowProcess.DownstreamStepHasProgress"
}
```

> Os GUIDs dos exemplos são ilustrativos; os formatos e nomes dos campos refletem fielmente o contrato real.

---

## 16. Gaps, limitações e decisões abertas

Os itens abaixo foram observados como **divergências entre o que está implementado no código e o que a spec/logic descrevem**, ou pontos que precisam de atenção do frontend. **Não são correções automáticas — são gaps reais que devem ser revisados pelo dev/produto.**

### Divergências spec × código

1. **Board retorna processos `Completed`** (gap funcional)
   - **Spec/logic dizem**: o board mostra **apenas** processos `InProgress` da contabilidade ativa; `Completed` vai para vista de histórico separada (`societario-process-history`).
   - **Código atual** (`GetProcessBoardHandler.cs`): filtra por `AccountancyId`, `IsDeleted == false` e opcionalmente `ProcessType`. **Não filtra por `Status`**.
   - **Impacto frontend**: até a correção, o frontend pode receber cards `Completed` no board e precisará filtrá-los localmente (ou aceitar a mistura). Documentado no exemplo de resposta.

2. **Skip não valida `IsOptional = true`** (gap de regra de negócio)
   - **Spec/logic dizem**: `SkipProcessTask` exige `IsOptional = true` — tasks obrigatórias não podem ser puladas.
   - **Código atual** (`SkipProcessTaskHandler.cs`): valida task Pending, step atual e task obrigatória anterior, **mas não rejeita skip de task `IsOptional = false`**.
   - **Impacto frontend**: a UI deve esconder/desabilitar a ação de skip para tasks obrigatórias. Não confiar no backend.

3. **Fill de campo exige "step atual"** (gap de regra de negócio)
   - **Spec/logic dizem**: campos podem ser preenchidos a qualquer momento, independentemente do status do step ou da task.
   - **Código atual** (`FillProcessStepFieldHandler.cs` e `FillProcessTaskFieldHandler.cs`): inclui a validação `currentStep.Id != request.StepInstanceId → StepNotUnlocked`, ou seja, só permite preencher campos do step atual.
   - **Impacto frontend**: campos de steps já Completed ou de steps NotStarted não podem ser preenchidos hoje. UI deve refletir isso ou tratar `409 StepNotUnlocked`.

4. **Primeiro step não nasce `InProgress`** (gap leve de transição)
   - **Spec/logic dizem**: na criação, o primeiro step fica `InProgress` imediatamente; demais ficam `NotStarted`.
   - **Código atual**: todas as steps materializadas (incluindo a primeira) nascem `NotStarted`. A transição para `InProgress` só ocorre quando a primeira ação (complete/skip) é executada sobre uma task daquele step (via `ProcessStepInstanceStarted` em `CompleteTask`/`SkipTask` no aggregate).
   - **Impacto frontend**: na primeira renderização do detalhe pós-criação, o "step atual" exibido pelo board (`currentStepTitle`) ainda funciona porque é o primeiro step não-Completed independentemente do `Status`. No `GetProcessDetail`, todas as steps virão `NotStarted` até a primeira ação. UI deve tolerar isso.

### Limitações conhecidas

5. **Não há paginação no board.** Em contabilidades com muitos processos, a lista pode crescer arbitrariamente.

6. **Não há filtro por status, período ou sócio no board** — apenas `processType`. Outras filtragens são responsabilidade do frontend (in-memory) por enquanto.

7. **Sem auditoria por `userId`.** Decidido em produto (spec §6/§8). Apenas timestamps são registrados (`CompletedAt`, `SkippedAt`, `FilledAt`).

8. **`templateVersion` é string opaca.** Hoje vem do `WorkflowTemplate.Version` cru. Não há contrato sobre formato (semver, monotônico, etc.). Frontend não deve interpretar.

9. **Fields são opacos quanto ao `value`.** Backend não valida formato de `Boolean`, `Integer`, `Date`, `Select`. Frontend é responsável por serializar e validar.

10. **Mensagens de erro estão em inglês.** Conforme regra do projeto. Frontend pode optar por mostrá-las literalmente ou mapear para PT-BR localmente.

### Pontos abertos / a decidir

11. **Tratamento de processos `Completed` no board** (consequência do gap #1): manter no board até o histórico ser construído? Ou já filtrar agora? — **decisão pendente do dev/produto**.

12. **Re-preenchimento de campo gera `FilledAt` novo a cada chamada.** Não há histórico de valores anteriores (decisão de design). Confirmar se isso atende o uso esperado.

13. **Não há endpoint de "obter board agrupado por step"** — a spec menciona "agrupados por step" (decisão §8 da spec), mas o `ProcessBoardItemResponse` é uma lista plana. O agrupamento por step (kanban visual) é responsabilidade do frontend usando `currentStepTitle` + `completedSteps`/`totalSteps`.

14. **Não há validação de duplicidade na criação** (mesmo template + mesmo cliente + mesma contabilidade). Decisão atual: pode haver processos duplicados; é aceitável (cliente-alvo é texto livre).

15. **`WorkflowProcessCompleted` é publicado via EventBus** (MassTransit/RabbitMQ) e consumido somente pela própria projeção. Quando `societario-template-versioning` for construído, será consumidor adicional. Sem ações pendentes hoje, mas vale registrar.

---
