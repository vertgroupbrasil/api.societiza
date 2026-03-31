# Domain Model — Societiza

> Generated on 2026-03-25.
> Updated on 2026-03-31 to reflect the full verified implementation of all modules.
> Serves as the canonical reference for development, architecture review, and Claude Code context.

---

## Module Overview

```
Core.Domain           → Shared primitives (Entity, AggregateRoot)
Modules/
  ├── Accountancy      → Accounting firm (tenant root)
  ├── Identity         → Users, invitations, and authentication
  └── WorkflowTemplate → Corporate workflow templates
```

---

## Core.Domain — Primitives

All aggregates and entities inherit from these base types.

**`Entity`**
- `Id` (`Guid`)
- `CreatedAt` (`DateTimeOffset`)
- `UpdatedAt` (`DateTimeOffset?`)
- `IsDeleted` (`bool`)

**`AggregateRoot`** — inherits `Entity`
- `Version` (`ulong`)
- Queue of `IDomainEvent`

---

## Module: Accountancy

### Aggregates

#### `Accountancy` — `<<AggregateRoot>>`

Represents an accounting firm. It is the tenant root of the system — all processes and users belong to a firm.

- `Id` (`Guid`): inherited from `Entity`
- `CNPJ` (`string`): fiscal identifier of the firm
- `LegalName` (`string`): legal name
- `TradeName` (`string?`): trade name (optional)
- `Address` (`string`): full address
- `City` (`string`): city
- `State` (`string`): state (UF)
- `PostalCode` (`string`): postal code
- `Phone` (`string`): contact phone
- `Email` (`string?`): contact email (optional)

**Factory method:** `Accountancy.Create(cnpj, legalName, tradeName, address, city, state, postalCode, phone, email)` — fires `AccountancyCreated`.

**Behavior methods:**
- `Update(cnpj, legalName, tradeName, address, city, state, postalCode, phone, email)` — fires `AccountancyUpdated`
- `Delete()` — fires `AccountancyDeleted`

---

### Domain Events

All events are `sealed record : Message, IDomainEvent`.

- `AccountancyCreated`: `Id, CNPJ, LegalName, TradeName, Address, City, State, PostalCode, Phone, Email, CreatedAt, Version`
- `AccountancyUpdated`: `Id, CNPJ, LegalName, TradeName, Address, City, State, PostalCode, Phone, Email, UpdatedAt, Version`
- `AccountancyDeleted`: `Id, Version`

---

### Projection Models

Read models for MongoDB, all `record : IProjectionModel`.

- `ProjectionModel.Accountancy`: `Id, CNPJ, LegalName, TradeName, Address, City, State, PostalCode, Phone, Email, CreatedAt, UpdatedAt, IsDeleted`

---

## Module: Identity

### Overview

The Identity module manages the complete access lifecycle: invitation, registration, and authentication. The canonical flow is `UserInvite → User` — a `User` can only be created from an accepted invitation.

### Aggregates and Relationships

```
UserInvite (AggregateRoot)   ──── (0..1) User (AggregateRoot)
  lifecycle: Pending → Accepted | Revoked | Expired
  via UserRegistrationCompleted.InviteId
```

### Aggregates

#### `User` — `<<AggregateRoot>>`

Represents a accountant or administrator who operates the system. **Created exclusively via `User.Create()` after a `UserInvite` is accepted.** Always linked to a firm, except `SystemAdmin`.

- `Id` (`Guid`): inherited from `Entity`
- `FirstName` (`string`): first name
- `LastName` (`string`): last name
- `Email` (`string`): login email (unique in the system)
- `PasswordHash` (`string?`): hashed password (nullable)
- `Role` (`UserRole`): access profile (SmartEnum)
- `AccountancyId` (`Guid?`): reference to the firm (null for `SystemAdmin`)

**Factory method:** `User.Create(inviteId, firstName, lastName, email, passwordHash, role, accountancyId)` — fires `UserRegistrationCompleted`.

**State reconstitution:** `ApplyEvent` delegates to `When(@event as dynamic)`. The single handler `When(UserRegistrationCompleted)` populates all properties, converting `Role` with `UserRole.FromName()`.

---

#### `UserInvite` — `<<AggregateRoot>>`

Represents an invitation sent to a future system user. Controls the access lifecycle: token generation, expiry, acceptance, and revocation.

- `Id` (`Guid`): inherited from `Entity`
- `Email` (`string`): invitee's email
- `Role` (`UserRole`): profile to be assigned to the user
- `AccountancyId` (`Guid?`): firm the user will be linked to
- `TokenHash` (`string`): SHA-256 hash of the invitation token (never the raw token)
- `Status` (`InviteStatus`): current state (SmartEnum)
- `ExpiresAt` (`DateTimeOffset`): expiry — `UtcNow + 72h` from creation or resend
- `UsedAt` (`DateTimeOffset?`): moment of acceptance or revocation

**Factory method:** `UserInvite.Create(email, role, accountancyId, tokenHash)` — instantiates with `Status = Pending` and `ExpiresAt = UtcNow + 72h`, fires `UserInviteCreated`.

**Behavior methods:**

- `IsValid()` — query; returns `true` if `Status == Pending && ExpiresAt > UtcNow`
- `Accept()` — transition `Pending → Accepted`; fires `UserInviteAccepted`
- `Revoke()` — transition `Pending → Revoked`; fires `UserInviteRevoked`
- `Expire()` — transition `Pending → Expired`; fires `UserInviteExpired`
- `Resend(newTokenHash)` — renews token and resets `ExpiresAt = UtcNow + 72h`; fires `UserInviteResent`

**State reconstitution:** 5 `When(...)` handlers, one per `UserInvite` event.

---

### Domain Events

All events are `sealed record : Message, IDomainEvent`. `Role` and `Status` are transported as `string` in events — conversion to SmartEnum occurs in the aggregates' `When(...)` handlers.

- `UserInviteCreated`: `Id, Email, Role, AccountancyId, TokenHash, Status, ExpiresAt, CreatedAt, Version`
- `UserInviteResent`: `Id, TokenHash, ExpiresAt, UpdatedAt, Version`
- `UserInviteAccepted`: `Id, Status, UsedAt, UpdatedAt, Version`
- `UserInviteRevoked`: `Id, Status, UsedAt, UpdatedAt, Version`
- `UserInviteExpired`: `Id, Status, UsedAt, UpdatedAt, Version`
- `UserRegistrationCompleted`: `Id, InviteId, FirstName, LastName, Email, PasswordHash, Role, AccountancyId, CreatedAt, Version`

---

### Projection Models

Read models for MongoDB, all `record : IProjectionModel`.

- `ProjectionModel.User`: `Id, FirstName, LastName, Email, Role, AccountancyId, CreatedAt, UpdatedAt, IsDeleted`
- `ProjectionModel.UserInvite`: `Id, Email, Role, AccountancyId, TokenHash, Status, ExpiresAt, UsedAt, CreatedAt, UpdatedAt, IsDeleted`

> `TokenHash` is stored in the projection (SHA-256 hash), never the raw token.

---

### Enumerations

#### `UserRole` — `<<SmartEnum>>`

Implemented with `Ardalis.SmartEnum`. Implicit conversions to/from `string` (via `Name`) and `int` (via `Value`). Each value is an internal `sealed` subclass.

- `SystemAdmin` (0): global platform administrator of Societiza
- `AccountingAdmin` (1): accounting firm administrator
- `AccountingEmployee` (2): firm employee

#### `InviteStatus` — `<<SmartEnum>>`

Controls the lifecycle of `UserInvite`. Same SmartEnum pattern + implicit `string`/`int` conversions.

- `Pending` (0): invitation sent, awaiting acceptance
- `Accepted` (1): invitation accepted, user created
- `Expired` (2): 72h window expired without acceptance
- `Revoked` (3): invitation manually cancelled

---

## Module: WorkflowTemplate

### Overview

Module responsible for defining corporate workflow templates. A template defines the steps, tasks, and fields that make up a type of process (e.g. company opening). Templates are reused to create process instances.

### Aggregates and Entities

```
WorkflowTemplate (AggregateRoot)
└── WorkflowTemplateStep (Entity)  [1..*]
    ├── TemplateStepField (Entity)  [0..*]
    └── WorkflowTemplateStepTask (Entity)  [1..*]
        └── TemplateStepTaskField (Entity)  [0..*]
```

---

#### `WorkflowTemplate` — `<<AggregateRoot>>`

Defines a complete corporate workflow template (e.g. "MEI Opening", "Partner Change").

- `Id` (`Guid`): inherited from `Entity`
- `Name` (`string`): template name
- `Description` (`string`): description of the template's purpose
- `Version` (`TemplateVersion`): semantic versioning of the template
- `Status` (`TemplateStatus`): template lifecycle state
- `Steps` (`List<WorkflowTemplateStep>`): ordered workflow steps

**Factory method:** `WorkflowTemplate.Create(name, description)` — fires `WorkflowTemplateCreated`.

**Behavior methods:**
- `Update(name, description)` — fires `WorkflowTemplateUpdated`
- `Activate()` — transition `Draft → Active`; fires `WorkflowTemplateActivated`
- `Archive()` — transition `Active → Archived`; fires `WorkflowTemplateArchived`
- `AddStep(title, description, order)` — fires `WorkflowTemplateStepAdded`
- `UpdateStep(stepId, title, description, order)` — fires `WorkflowTemplateStepUpdated`
- `RemoveStep(stepId)` — fires `WorkflowTemplateStepRemoved`
- `AddTask(stepId, title, description, type, configuration, order, isOptional)` — fires `WorkflowTemplateStepTaskAdded`
- `UpdateTask(stepId, taskId, title, description, type, configuration, order, isOptional)` — fires `WorkflowTemplateStepTaskUpdated`
- `RemoveTask(stepId, taskId)` — fires `WorkflowTemplateStepTaskRemoved`
- `AddStepField(stepId, label, fieldType, options)` — fires `WorkflowTemplateStepFieldAdded`
- `UpdateStepField(stepId, fieldId, label, fieldType, options)` — fires `WorkflowTemplateStepFieldUpdated`
- `RemoveStepField(stepId, fieldId)` — fires `WorkflowTemplateStepFieldRemoved`
- `AddTaskField(stepId, taskId, label, fieldType, options)` — fires `WorkflowTemplateStepTaskFieldAdded`
- `UpdateTaskField(stepId, taskId, fieldId, label, fieldType, options)` — fires `WorkflowTemplateStepTaskFieldUpdated`
- `RemoveTaskField(stepId, taskId, fieldId)` — fires `WorkflowTemplateStepTaskFieldRemoved`

---

#### `WorkflowTemplateStep` — `<<Entity>>`

Represents a step within the template (e.g. "Viability Check", "Registration", "Operating Licence"). Belongs to a single `WorkflowTemplate`.

- `Id` (`Guid`): inherited from `Entity`
- `TemplateId` (`Guid`): FK to the parent template
- `Title` (`string`): step title
- `Description` (`string`): step description
- `Order` (`int`): position in the flow (ordering)
- `Fields` (`List<TemplateStepField>`): data fields for the step
- `Tasks` (`List<WorkflowTemplateStepTask>`): tasks to execute at this step

---

#### `WorkflowTemplateStepTask` — `<<Entity>>`

Represents a task within a step (e.g. "Fill out viability form", "Submit DARE"). Can be manual, automatic, or approval-based.

- `Id` (`Guid`): inherited from `Entity`
- `Title` (`string`): task title
- `Description` (`string`): description of what must be done
- `Type` (`TaskType`): nature of task execution
- `Configuration` (`string`): serialized configuration (e.g. automation parameters); defaults to `{}`
- `Order` (`int`): position within the step
- `IsOptional` (`bool`): whether the task can be skipped
- `Fields` (`List<TemplateStepTaskField>`): data fields for the task

---

#### `TemplateStepField` — `<<Entity>>`

Data field associated with a template step. Defines the schema of data collected at that step.

- `Id` (`Guid`): inherited from `Entity`
- `StepId` (`Guid`): FK to the parent step
- `Label` (`string`): label displayed to the user
- `FieldType` (`FieldType`): data type
- `Options` (`string?`): serialized options (used when `FieldType = Select`)

---

#### `TemplateStepTaskField` — `<<Entity>>`

Data field associated with a template task. Functions analogously to `TemplateStepField`, but scoped to a specific task.

- `Id` (`Guid`): inherited from `Entity`
- `TaskId` (`Guid`): FK to the parent task
- `Label` (`string`): label displayed to the user
- `FieldType` (`FieldType`): data type
- `Options` (`string?`): serialized options (used when `FieldType = Select`)

---

### Value Objects

#### `TemplateVersion` — `<<ValueObject>>`

Semantic versioning of a template. Immutable. Allows tracking template evolutions without breaking in-progress processes.

- `Major` (`int`)
- `Minor` (`int`)

> `ToString()` returns `"v{Major}.{Minor}"`. Use `Parse()` for deserialization.

---

### Domain Events

All events are `sealed record : Message, IDomainEvent`. `Status` and `Type` are transported as `string` — conversion to enumerations occurs in the projection handlers.

**Template lifecycle:**
- `WorkflowTemplateCreated`: `Id, Name, Description, TemplateVersion, Status, CreatedAt, Version`
- `WorkflowTemplateUpdated`: `Id, Name, Description, UpdatedAt, Version`
- `WorkflowTemplateActivated`: `Id, UpdatedAt, Version`
- `WorkflowTemplateArchived`: `Id, UpdatedAt, Version`

**Steps:**
- `WorkflowTemplateStepAdded`: `TemplateId, StepId, Title, Description, Order, CreatedAt, Version`
- `WorkflowTemplateStepUpdated`: `TemplateId, StepId, Title, Description, Order, UpdatedAt, Version`
- `WorkflowTemplateStepRemoved`: `TemplateId, StepId, FieldIds, TaskIds, TaskFieldIds, Version`

**Tasks:**
- `WorkflowTemplateStepTaskAdded`: `TemplateId, StepId, TaskId, Title, Description, Type, Configuration, Order, IsOptional, CreatedAt, Version`
- `WorkflowTemplateStepTaskUpdated`: `TemplateId, StepId, TaskId, Title, Description, Type, Configuration, Order, IsOptional, UpdatedAt, Version`
- `WorkflowTemplateStepTaskRemoved`: `TemplateId, StepId, TaskId, TaskFieldIds, Version`

**Step fields:**
- `WorkflowTemplateStepFieldAdded`: `TemplateId, StepId, FieldId, Label, FieldType, Options, CreatedAt, Version`
- `WorkflowTemplateStepFieldUpdated`: `TemplateId, StepId, FieldId, Label, FieldType, Options, UpdatedAt, Version`
- `WorkflowTemplateStepFieldRemoved`: `TemplateId, StepId, FieldId, Version`

**Task fields:**
- `WorkflowTemplateStepTaskFieldAdded`: `TemplateId, StepId, TaskId, FieldId, Label, FieldType, Options, CreatedAt, Version`
- `WorkflowTemplateStepTaskFieldUpdated`: `TemplateId, StepId, TaskId, FieldId, Label, FieldType, Options, UpdatedAt, Version`
- `WorkflowTemplateStepTaskFieldRemoved`: `TemplateId, StepId, TaskId, FieldId, Version`

---

### Projection Models

Read models for MongoDB, all `record : IProjectionModel`.

- `ProjectionModel.WorkflowTemplate`: `Id, Name, Description, TemplateVersion (string), Status (string), CreatedAt, UpdatedAt, IsDeleted`
- `ProjectionModel.WorkflowTemplateStep`: `Id, TemplateId, Title, Description, Order, CreatedAt, UpdatedAt`
- `ProjectionModel.WorkflowTemplateStepTask`: `Id, TemplateId, StepId, Title, Description, Type (string), Configuration, Order, IsOptional, CreatedAt, UpdatedAt`
- `ProjectionModel.TemplateStepField`: `Id, TemplateId, StepId, Label, FieldType (string), Options, CreatedAt, UpdatedAt`
- `ProjectionModel.TemplateStepTaskField`: `Id, TemplateId, StepId, TaskId, Label, FieldType (string), Options, CreatedAt, UpdatedAt`

---

### Enumerations

#### `TemplateStatus`

- `Draft` (0): under construction, not available for use
- `Active` (1): published and available to create processes
- `Archived` (2): discontinued, read-only

#### `TaskType`

- `Manual` (0): executed manually by the operator
- `Automatic` (1): executed automatically by the system (e.g. portal automation)
- `Approval` (2): requires explicit approval before advancing

#### `FieldType`

- `Text` (0): free text
- `Boolean` (1): true / false
- `Integer` (2): whole number
- `Select` (3): selection from pre-defined options
- `Date` (4): date

---

## Model Summary

3 modules (Accountancy, Identity, WorkflowTemplate), 4 aggregates (Accountancy, User, UserInvite, WorkflowTemplate), 4 entities (WorkflowTemplateStep, WorkflowTemplateStepTask, TemplateStepField, TemplateStepTaskField), 1 value object (TemplateVersion), 5 enumerations (UserRole, InviteStatus, TemplateStatus, TaskType, FieldType — SmartEnum in Identity), 25 domain events (3 Accountancy + 6 Identity + 16 WorkflowTemplate), 8 projection models (1 Accountancy + 2 Identity + 5 WorkflowTemplate).

---

## Domain Rules

### Accountancy
- An `Accountancy` firm is the tenant root — all processes and users belong to a firm.

### Identity
- `User` can only be created via `User.Create()` after a `UserInvite` is accepted — no direct creation.
- `User.AccountancyId` is `null` only for `SystemAdmin`. Every other role requires a firm link.
- `UserStatus` was **removed** — access state is managed by `UserInvite.Status` (`InviteStatus`).
- `User.Name` was **replaced** by `FirstName` + `LastName`.
- `UserInvite.IsValid()` returns `true` only if `Status == Pending && ExpiresAt > UtcNow`.
- The invitation token is **never stored in plain text** — only the SHA-256 hash (`TokenHash`), both in the aggregate and the projection.
- `UserInvite.Resend()` renews the `TokenHash` and resets `ExpiresAt = UtcNow + 72h`, keeping the same aggregate.
- `Role` and `Status` travel as `string` in domain events; conversion to SmartEnum occurs in the aggregates' `When(...)` handlers.
- Identity module enumerations use `Ardalis.SmartEnum` with implicit `string`/`int` conversions.

### WorkflowTemplate
- A `WorkflowTemplate` in `Draft` status cannot be used to create processes.
- `TemplateVersion` is an immutable Value Object — any structural change must generate a new version.
- `WorkflowTemplateStepTask.Configuration` stores serialized (JSON) configuration specific to each `TaskType`. For `Automatic` tasks, it contains the automation parameters.
- Step and task ordering is controlled by the `Order` field (integer). Reordering logic is the Application layer's responsibility.
- `TemplateStepField.Options` and `TemplateStepTaskField.Options` are used exclusively when `FieldType = Select`. The serialization format must be defined and documented in the Application layer.
- `WorkflowTemplateStepRemoved` carries `FieldIds`, `TaskIds`, and `TaskFieldIds` to allow cascading deletion in projections.

---

*Updated on 2026-03-31.*