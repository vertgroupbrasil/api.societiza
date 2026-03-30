# Domain Model — Societiza

> Documento gerado em 25/03/2026 a partir do diagrama visual (`Domain_Model_-_Societiza.excalidraw`) e do mapeamento textual (`SOCIETIZA_DOMAIN_MODEL.md`).
> Atualizado em 29/03/2026 com a implementação real do módulo Identity.
> Serve como referência canônica para desenvolvimento, revisão de arquitetura e contexto para o Claude Code.

---

## Visão Geral dos Módulos

```
Core.Domain           → Primitivos compartilhados (Entity, AggregateRoot)
Modules/
  ├── Accountancy      → Escritório de contabilidade
  ├── Identity         → Usuários, convites e autenticação
  └── WorkflowTemplate → Templates de fluxo societário
```

---

## Core.Domain — Primitivos

Todos os aggregates e entidades herdam destes tipos base.

| Tipo | Nome | Propriedades |
|---|---|---|
| Base | `Entity` | `Guid Id`, `DateTimeOffset CreatedAt`, `DateTimeOffset? UpdatedAt`, `bool IsDeleted` |
| Base | `AggregateRoot` | Herda `Entity` + `ulong Version`, queue de `IDomainEvent` |

---

## Module: Accountancy

### Aggregates

#### `Accountancy` — `<<AggregateRoot>>`

Representa um escritório de contabilidade. É o tenant raiz do sistema — todos os processos e usuários pertencem a um escritório.

| Propriedade | Tipo | Observação |
|---|---|---|
| `Id` | `Guid` | Herdado de `Entity` |
| `CNPJ` | `string` | Identificador fiscal do escritório |
| `LegalName` | `string` | Razão social |
| `TradeName` | `string?` | Nome fantasia (opcional) |
| `Address` | `string` | Endereço completo |
| `City` | `string` | Município |
| `State` | `string` | UF |
| `PostalCode` | `string` | CEP |
| `Phone` | `string` | Telefone de contato |
| `Email` | `string?` | E-mail de contato (opcional) |

---

## Module: Identity

### Visão Geral

O módulo Identity gerencia o ciclo de vida completo de acesso ao sistema: convite, registro e autenticação. O fluxo canônico é `UserInvite → User` — um `User` só pode ser criado a partir de um convite aceito.

### Agregados e Relacionamentos

```
UserInvite (AggregateRoot)   ──── (0..1) User (AggregateRoot)
  lifecycle: Pending → Accepted | Revoked | Expired
  via UserRegistrationCompleted.InviteId
```

### Aggregates

#### `User` — `<<AggregateRoot>>`

Representa um contador ou administrador que opera o sistema. **Criado exclusivamente via `User.Create()` após a aceitação de um `UserInvite`.** Sempre vinculado a um escritório, exceto `SystemAdmin`.

| Propriedade | Tipo | Observação |
|---|---|---|
| `Id` | `Guid` | Herdado de `Entity` |
| `FirstName` | `string` | Primeiro nome |
| `LastName` | `string` | Sobrenome |
| `Email` | `string` | E-mail de login (único no sistema) |
| `PasswordHash` | `string?` | Senha hasheada (nullable) |
| `Role` | `UserRole` | Perfil de acesso (SmartEnum) |
| `AccountancyId` | `Guid?` | Referência ao escritório (null para `SystemAdmin`) |

> **Nota de implementação:** `Name` foi substituído por `FirstName` + `LastName`. `Status` (UserStatus) foi removido do aggregate — o estado de acesso é gerenciado pelo `UserInvite`. `PasswordHash` é `string?` (nullable).

**Factory method:** `User.Create(inviteId, firstName, lastName, email, passwordHash, role, accountancyId)` — instancia o aggregate e dispara `UserRegistrationCompleted`.

**Reconstituição de estado:** `ApplyEvent` delega para `When(@event as dynamic)`. O único handler `When(UserRegistrationCompleted)` popula todas as propriedades, convertendo `Role` com `UserRole.FromName()`.

---

#### `UserInvite` — `<<AggregateRoot>>`

Representa um convite enviado a um futuro usuário do sistema. Controla o ciclo de vida de acesso: geração do token, expiração, aceitação e revogação.

| Propriedade | Tipo | Observação |
|---|---|---|
| `Id` | `Guid` | Herdado de `Entity` |
| `Email` | `string` | E-mail do convidado |
| `Role` | `UserRole` | Perfil que será atribuído ao usuário |
| `AccountancyId` | `Guid?` | Escritório ao qual o usuário será vinculado |
| `TokenHash` | `string` | Hash SHA-256 do token de convite (nunca o raw token) |
| `Status` | `InviteStatus` | Estado atual do convite (SmartEnum) |
| `ExpiresAt` | `DateTimeOffset` | Expiração: `UtcNow + 72h` a partir da criação ou reenvio |
| `UsedAt` | `DateTimeOffset?` | Momento da aceitação ou revogação |

**Factory method:** `UserInvite.Create(email, role, accountancyId, tokenHash)` — instancia com `Status = Pending` e `ExpiresAt = UtcNow + 72h`, dispara `UserInviteCreated`.

**Métodos de comportamento:**

| Método | Transição | Evento disparado |
|---|---|---|
| `IsValid()` | — (consulta) | `Status == Pending && ExpiresAt > UtcNow` |
| `Accept()` | `Pending → Accepted` | `UserInviteAccepted` |
| `Revoke()` | `Pending → Revoked` | `UserInviteRevoked` |
| `Expire()` | `Pending → Expired` | `UserInviteExpired` |
| `Resend(newTokenHash)` | — (renova token) | `UserInviteResent` (novo hash + `ExpiresAt = UtcNow + 72h`) |

**Reconstituição de estado:** 5 handlers `When(...)`, um por evento de `UserInvite`.

---

### Domain Events

Todos os eventos são `sealed record : Message, IDomainEvent`. `Role` e `Status` são transportados como `string` nos eventos — a conversão para SmartEnum ocorre nos handlers `When(...)` dos aggregates.

| Evento | Campos |
|---|---|
| `UserInviteCreated` | `Id, Email, Role, AccountancyId, TokenHash, Status, ExpiresAt, CreatedAt, Version` |
| `UserInviteResent` | `Id, TokenHash, ExpiresAt, UpdatedAt, Version` |
| `UserInviteAccepted` | `Id, Status, UsedAt, UpdatedAt, Version` |
| `UserInviteRevoked` | `Id, Status, UsedAt, UpdatedAt, Version` |
| `UserInviteExpired` | `Id, Status, UsedAt, UpdatedAt, Version` |
| `UserRegistrationCompleted` | `Id, InviteId, FirstName, LastName, Email, PasswordHash, Role, AccountancyId, CreatedAt, Version` |

---

### Projection Models

Modelos de leitura para MongoDB, ambos `record : IProjectionModel`.

| Modelo | Campos |
|---|---|
| `ProjectionModel.User` | `Id, FirstName, LastName, Email, Role, AccountancyId, CreatedAt, UpdatedAt, IsDeleted` |
| `ProjectionModel.UserInvite` | `Id, Email, Role, AccountancyId, TokenHash, Status, ExpiresAt, UsedAt, CreatedAt, UpdatedAt, IsDeleted` |

> `TokenHash` é armazenado na projeção (hash SHA-256), nunca o raw token.

---

### Enumerations

#### `UserRole` — `<<SmartEnum>>`

Implementado com `Ardalis.SmartEnum`. Conversões implícitas de/para `string` (via `Name`) e `int` (via `Value`). Cada valor é uma subclasse interna `sealed`.

| Valor | Value | Descrição |
|---|---|---|
| `SystemAdmin` | 0 | Administrador global da plataforma Societiza |
| `AccountingAdmin` | 1 | Administrador do escritório de contabilidade |
| `AccountingEmployee` | 2 | Colaborador do escritório |

> **Nota:** `UserStatus` foi **removido** do modelo — substituído por `InviteStatus` no aggregate `UserInvite`.

#### `InviteStatus` — `<<SmartEnum>>`

Controla o ciclo de vida do `UserInvite`. Mesmo padrão SmartEnum + conversões implícitas `string`/`int`.

| Valor | Value | Descrição |
|---|---|---|
| `Pending` | 0 | Convite enviado, aguardando aceitação |
| `Accepted` | 1 | Convite aceito, usuário criado |
| `Expired` | 2 | Prazo de 72h expirado sem aceitação |
| `Revoked` | 3 | Convite cancelado manualmente |

---

## Module: WorkflowTemplate

### Visão Geral

Módulo responsável pela definição dos templates de fluxo societário. Um template define as etapas, tarefas e campos que compõem um tipo de processo (ex: abertura de empresa). Os templates são reutilizados para criar instâncias de processo.

### Agregados e Entidades

```
WorkflowTemplate (AggregateRoot)
└── WorkflowTemplateStep (Entity)  [1..*]
    ├── TemplateStepField (Entity)  [0..*]
    └── WorkflowTemplateStepTask (Entity)  [1..*]
        └── TemplateStepTaskField (Entity)  [0..*]
```

---

#### `WorkflowTemplate` — `<<AggregateRoot>>`

Define um template completo de fluxo societário (ex: "Abertura de MEI", "Alteração de Sócio").

| Propriedade | Tipo | Observação |
|---|---|---|
| `Id` | `Guid` | Herdado de `Entity` |
| `Name` | `string` | Nome do template |
| `Description` | `string` | Descrição do propósito do template |
| `Version` | `TemplateVersion` | Versionamento semântico do template |
| `Status` | `TemplateStatus` | Ciclo de vida do template |
| `Steps` | `List<WorkflowTemplateStep>` | Etapas ordenadas do fluxo |

---

#### `WorkflowTemplateStep` — `<<Entity>>`

Representa uma etapa dentro do template (ex: "Viabilidade", "Registro", "Alvará"). Pertence a um único `WorkflowTemplate`.

| Propriedade | Tipo | Observação |
|---|---|---|
| `Id` | `Guid` | Herdado de `Entity` |
| `TemplateId` | `Guid` | FK para o template pai |
| `Title` | `string` | Título da etapa |
| `Description` | `string` | Descrição da etapa |
| `Order` | `int` | Posição no fluxo (ordenação) |
| `Fields` | `List<TemplateStepField>` | Campos de dados da etapa |
| `Tasks` | `List<WorkflowTemplateStepTask>` | Tarefas a executar nesta etapa |

---

#### `WorkflowTemplateStepTask` — `<<Entity>>`

Representa uma tarefa dentro de uma etapa (ex: "Preencher formulário de viabilidade", "Enviar DARE"). Pode ser manual, automática ou de aprovação.

| Propriedade | Tipo | Observação |
|---|---|---|
| `Id` | `Guid` | Herdado de `Entity` |
| `Title` | `string` | Título da tarefa |
| `Description` | `string` | Descrição do que deve ser feito |
| `Type` | `TaskType` | Natureza da execução da tarefa |
| `Configuration` | `string` | Configuração serializada (ex: parâmetros de automação) |
| `Order` | `int` | Posição dentro da etapa |
| `IsOptional` | `bool` | Se a tarefa pode ser pulada |
| `Fields` | `List<TemplateStepTaskField>` | Campos de dados da tarefa |

---

#### `TemplateStepField` — `<<Entity>>`

Campo de dado associado a uma etapa do template. Define o schema dos dados coletados naquela etapa.

| Propriedade | Tipo | Observação |
|---|---|---|
| `Id` | `Guid` | Herdado de `Entity` |
| `StepId` | `Guid` | FK para a etapa pai |
| `Label` | `string` | Rótulo exibido ao usuário |
| `FieldType` | `FieldType` | Tipo do dado |
| `Options` | `string?` | Opções serializadas (usado quando `FieldType = Select`) |

---

#### `TemplateStepTaskField` — `<<Entity>>`

Campo de dado associado a uma tarefa do template. Funciona de forma análoga ao `TemplateStepField`, mas no escopo de uma tarefa específica.

| Propriedade | Tipo | Observação |
|---|---|---|
| `Id` | `Guid` | Herdado de `Entity` |
| `TaskId` | `Guid` | FK para a tarefa pai |
| `Label` | `string` | Rótulo exibido ao usuário |
| `FieldType` | `FieldType` | Tipo do dado |
| `Options` | `string?` | Opções serializadas (usado quando `FieldType = Select`) |

---

### Value Objects

#### `TemplateVersion` — `<<ValueObject>>`

Versionamento semântico de um template. Imutável. Permite rastrear evoluções do template sem quebrar processos em andamento.

| Propriedade | Tipo |
|---|---|
| `Major` | `int` |
| `Minor` | `int` |

---

### Enumerations

#### `TemplateStatus`

| Valor | Descrição |
|---|---|
| `Draft` | Em construção, não disponível para uso |
| `Active` | Publicado e disponível para criar processos |
| `Archived` | Descontinuado, somente leitura |

#### `TaskType`

| Valor | Descrição |
|---|---|
| `Manual` | Executada manualmente pelo operador |
| `Automatic` | Executada automaticamente pelo sistema (ex: automação de portal) |
| `Approval` | Requer aprovação explícita antes de avançar |

#### `FieldType`

| Valor | Descrição |
|---|---|
| `Text` | Texto livre |
| `Boolean` | Verdadeiro / Falso |
| `Integer` | Número inteiro |
| `Select` | Seleção a partir de opções pré-definidas |
| `Date` | Data |

---

## Resumo do Modelo

| Tipo | Quantidade |
|---|---|
| Módulos | 3 (Accountancy, Identity, WorkflowTemplate) |
| Aggregates | 4 (Accountancy, User, UserInvite, WorkflowTemplate) |
| Entities | 4 (WorkflowTemplateStep, WorkflowTemplateStepTask, TemplateStepField, TemplateStepTaskField) |
| Value Objects | 1 (TemplateVersion) |
| Enumerations | 6 (UserRole, InviteStatus, TemplateStatus, TaskType, FieldType) — *SmartEnum em Identity* |
| Domain Events | 6 (5 de UserInvite + 1 de User) |
| Projection Models | 2 (ProjectionModel.User, ProjectionModel.UserInvite) |

---

## Regras de Domínio Relevantes

### Identity
- `User` só pode ser criado via `User.Create()` após a aceitação de um `UserInvite` — não existe criação direta.
- `User.AccountancyId` é `null` apenas para `SystemAdmin`. Toda outra role exige vínculo com um escritório.
- `UserStatus` foi **removido** — o estado de acesso é gerenciado pelo `UserInvite.Status` (`InviteStatus`).
- `User.Name` foi **substituído** por `FirstName` + `LastName`.
- `UserInvite.IsValid()` retorna `true` apenas se `Status == Pending && ExpiresAt > UtcNow`.
- O token de convite **nunca é armazenado em texto puro** — apenas o hash SHA-256 (`TokenHash`), tanto no aggregate quanto na projeção.
- `UserInvite.Resend()` renova o `TokenHash` e redefine `ExpiresAt = UtcNow + 72h`, mantendo o mesmo aggregate.
- `Role` e `Status` trafegam como `string` nos domain events; a conversão para SmartEnum ocorre nos handlers `When(...)` dos aggregates.
- Enumerações do módulo Identity usam `Ardalis.SmartEnum` com conversões implícitas `string`/`int`.

### WorkflowTemplate
- Um `WorkflowTemplate` em status `Draft` não pode ser usado para criar processos.
- `TemplateVersion` é um Value Object imutável — qualquer alteração estrutural deve gerar uma nova versão.
- `WorkflowTemplateStepTask.Configuration` armazena configuração serializada (JSON) específica por `TaskType`. Para tarefas `Automatic`, contém os parâmetros da automação.
- A ordenação de etapas e tarefas é controlada pelo campo `Order` (inteiro). A lógica de reordenação é responsabilidade da camada de Application.
- `TemplateStepField.Options` e `TemplateStepTaskField.Options` são usados exclusivamente quando `FieldType = Select`. O formato de serialização deve ser definido e documentado na camada de Application.
