# Domain Model — Societiza

> Documento gerado em 25/03/2026 a partir do diagrama visual (`Domain_Model_-_Societiza.excalidraw`) e do mapeamento textual (`SOCIETIZA_DOMAIN_MODEL.md`).
> Serve como referência canônica para desenvolvimento, revisão de arquitetura e contexto para o Claude Code.

---

## Visão Geral dos Módulos

```
Core.Domain           → Primitivos compartilhados (Entity, AggregateRoot)
Modules/
  ├── Accountancy      → Escritório de contabilidade
  ├── Identity         → Usuários e autenticação
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

### Aggregates

#### `User` — `<<AggregateRoot>>`

Representa um contador ou administrador que opera o sistema. Sempre vinculado a um escritório (exceto `SystemAdmin`).

| Propriedade | Tipo | Observação |
|---|---|---|
| `Id` | `Guid` | Herdado de `Entity` |
| `Name` | `string` | Nome completo |
| `Email` | `string` | E-mail de login (único no sistema) |
| `PasswordHash` | `string` | Senha hasheada |
| `Role` | `UserRole` | Perfil de acesso |
| `Status` | `UserStatus` | Estado do convite/acesso |
| `AccountancyId` | `Guid?` | Referência ao escritório (null para `SystemAdmin`) |

### Enumerations

#### `UserRole`

| Valor | Descrição |
|---|---|
| `SystemAdmin` | Administrador global da plataforma Societiza |
| `AccountingAdmin` | Administrador do escritório de contabilidade |
| `AccountingEmployee` | Colaborador do escritório |

#### `UserStatus`

| Valor | Descrição |
|---|---|
| `Active` | Usuário ativo e com acesso liberado |
| `PendingInvite` | Convite enviado, acesso pendente de confirmação |
| `Inactive` | Usuário desativado |

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
| Aggregates | 3 (Accountancy, User, WorkflowTemplate) |
| Entities | 4 (WorkflowTemplateStep, WorkflowTemplateStepTask, TemplateStepField, TemplateStepTaskField) |
| Value Objects | 1 (TemplateVersion) |
| Enumerations | 5 (UserRole, UserStatus, TemplateStatus, TaskType, FieldType) |

---

## Regras de Domínio Relevantes

- `User.AccountancyId` é `null` apenas para `SystemAdmin`. Toda outra role exige vínculo com um escritório.
- `UserStatus.PendingInvite` indica que o usuário foi criado via convite mas ainda não confirmou o acesso.
- Um `WorkflowTemplate` em status `Draft` não pode ser usado para criar processos.
- `TemplateVersion` é um Value Object imutável — qualquer alteração estrutural deve gerar uma nova versão.
- `WorkflowTemplateStepTask.Configuration` armazena configuração serializada (JSON) específica por `TaskType`. Para tarefas `Automatic`, contém os parâmetros da automação.
- A ordenação de etapas e tarefas é controlada pelo campo `Order` (inteiro). A lógica de reordenação é responsabilidade da camada de Application.
- `TemplateStepField.Options` e `TemplateStepTaskField.Options` são usados exclusivamente quando `FieldType = Select`. O formato de serialização deve ser definido e documentado na camada de Application.
