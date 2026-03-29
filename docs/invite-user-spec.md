# Funcionalidade: Convite de Usuário

> Documento de estruturação técnica — preenchido incrementalmente.
> Última atualização: 26/03/2026

---

## Decisões de Design

- O `User` só é criado no momento em que o convidado **aceita** o convite
- `UserInvite` carrega os dados necessários para criar o usuário futuramente
- Sem estados fantasma — `User` sempre nasce ativo e consistente
- Sem senha aleatória — `PasswordHash` só existe quando o usuário define sua senha
- Design preparado para autenticação social (Google etc.) no futuro

---

## Modelo de Domínio

### `User` — `<<AggregateRoot>>`

| Propriedade | Tipo | Observação |
|---|---|---|
| `Id` | `Guid` | Herdado de `AggregateRoot` |
| `FirstName` | `string` | Preenchido na aceitação do convite |
| `LastName` | `string` | Preenchido na aceitação do convite |
| `Email` | `string` | Herdado do `UserInvite` |
| `PasswordHash` | `string` | Definido na aceitação do convite |
| `Role` | `UserRole` | Herdado do `UserInvite` |
| `AccountancyId` | `Guid?` | Nulo apenas para `SystemAdmin` |

**Removidos em relação à implementação anterior:**
- `Status` — substituído pelo `IsDeleted` herdado de `Entity`. Sem estado fantasma.

**Eventos de domínio:**
- `UserRegistrationCompleted` — emitido na aceitação do convite. Carrega `InviteId` no payload para rastreabilidade via event store.

---

### `UserInvite` — `<<AggregateRoot>>`

Tem identidade própria, ciclo de vida independente e muda de estado ao longo do tempo — critérios que justificam ser um `AggregateRoot` e não uma entidade filha de `User`.

| Propriedade | Tipo | Observação |
|---|---|---|
| `Id` | `Guid` | Herdado de `AggregateRoot` |
| `Email` | `string` | Email do convidado |
| `Role` | `UserRole` | Perfil que o usuário terá ao aceitar |
| `AccountancyId` | `Guid?` | Nulo apenas para convites de `SystemAdmin` |
| `TokenHash` | `string` | SHA256 do token cru — o token em si nunca é armazenado |
| `Status` | `InviteStatus` | Ciclo de vida do convite |
| `ExpiresAt` | `DateTimeOffset` | 72h a partir da criação ou do último reenvio |
| `UsedAt` | `DateTimeOffset?` | Preenchido quando aceito ou revogado |

**Métodos públicos:**
- `bool IsValid()` — `Status == Pending && ExpiresAt > UtcNow`
- `Accept()` — valida `Status == Pending`, chama `ChangeStatus(Accepted)`
- `Revoke()` — valida `Status == Pending`, chama `ChangeStatus(Revoked)`
- `Resend(newTokenHash, newExpiresAt)` — valida `Status == Pending`, atualiza token e expiração

**Método privado:**
- `ChangeStatus(InviteStatus)` — atualiza `Status` e `UsedAt = UtcNow`

**Eventos de domínio:**
- `UserInviteCreated`
- `UserInviteResent`
- `UserInviteAccepted`
- `UserInviteRevoked`
- `UserInviteExpired` — emitido pelo job de expiração

---

### Enumerations — Padrão SmartEnum (`Ardalis.SmartEnum`)

Todos os enumerations do módulo Identity seguem o padrão SmartEnum, com conversão implícita de/para `string` e `int`, garantindo serialização consistente no PostgreSQL e MongoDB sem configuração extra.

#### `UserRole`

```csharp
public class UserRole(string name, int value) : SmartEnum<UserRole>(name, value)
{
    public static readonly UserRole SystemAdmin = new SystemAdminRole();
    public static readonly UserRole AccountingAdmin = new AccountingAdminRole();
    public static readonly UserRole AccountingEmployee = new AccountingEmployeeRole();

    public static implicit operator UserRole(string name) => FromName(name);
    public static implicit operator UserRole(int value) => FromValue(value);
    public static implicit operator string(UserRole role) => role.Name;
    public static implicit operator int(UserRole role) => role.Value;

    public class SystemAdminRole() : UserRole(nameof(SystemAdmin), 0) { }
    public class AccountingAdminRole() : UserRole(nameof(AccountingAdmin), 1) { }
    public class AccountingEmployeeRole() : UserRole(nameof(AccountingEmployee), 2) { }
}
```

#### `InviteStatus`

```csharp
public class InviteStatus(string name, int value) : SmartEnum<InviteStatus>(name, value)
{
    public static readonly InviteStatus Pending = new PendingStatus();
    public static readonly InviteStatus Accepted = new AcceptedStatus();
    public static readonly InviteStatus Expired = new ExpiredStatus();
    public static readonly InviteStatus Revoked = new RevokedStatus();

    public static implicit operator InviteStatus(string name) => FromName(name);
    public static implicit operator InviteStatus(int value) => FromValue(value);
    public static implicit operator string(InviteStatus status) => status.Name;
    public static implicit operator int(InviteStatus status) => status.Value;

    public class PendingStatus() : InviteStatus(nameof(Pending), 0) { }
    public class AcceptedStatus() : InviteStatus(nameof(Accepted), 1) { }
    public class ExpiredStatus() : InviteStatus(nameof(Expired), 2) { }
    public class RevokedStatus() : InviteStatus(nameof(Revoked), 3) { }
}
```

---

### Enumerations — Padrão SmartEnum (`Ardalis.SmartEnum`)

Todos os enumerations do módulo Identity seguem o padrão SmartEnum, com conversão implícita de/para `string` e `int`, garantindo serialização consistente no PostgreSQL e MongoDB sem configuração extra.

#### `UserRole`

```csharp
public class UserRole(string name, int value) : SmartEnum<UserRole>(name, value)
{
    public static readonly UserRole SystemAdmin = new SystemAdminRole();
    public static readonly UserRole AccountingAdmin = new AccountingAdminRole();
    public static readonly UserRole AccountingEmployee = new AccountingEmployeeRole();

    public static implicit operator UserRole(string name) => FromName(name);
    public static implicit operator UserRole(int value) => FromValue(value);
    public static implicit operator string(UserRole role) => role.Name;
    public static implicit operator int(UserRole role) => role.Value;

    public class SystemAdminRole() : UserRole(nameof(SystemAdmin), 0) { }
    public class AccountingAdminRole() : UserRole(nameof(AccountingAdmin), 1) { }
    public class AccountingEmployeeRole() : UserRole(nameof(AccountingEmployee), 2) { }
}
```

#### `InviteStatus`

```csharp
public class InviteStatus(string name, int value) : SmartEnum<InviteStatus>(name, value)
{
    public static readonly InviteStatus Pending = new PendingStatus();
    public static readonly InviteStatus Accepted = new AcceptedStatus();
    public static readonly InviteStatus Expired = new ExpiredStatus();
    public static readonly InviteStatus Revoked = new RevokedStatus();

    public static implicit operator InviteStatus(string name) => FromName(name);
    public static implicit operator InviteStatus(int value) => FromValue(value);
    public static implicit operator string(InviteStatus status) => status.Name;
    public static implicit operator int(InviteStatus status) => status.Value;

    public class PendingStatus() : InviteStatus(nameof(Pending), 0) { }
    public class AcceptedStatus() : InviteStatus(nameof(Accepted), 1) { }
    public class ExpiredStatus() : InviteStatus(nameof(Expired), 2) { }
    public class RevokedStatus() : InviteStatus(nameof(Revoked), 3) { }
}
```

---

## Fluxo de Negócio

### Enviar convite

> ⚠️ Validação de permissão não implementada neste momento — será adicionada quando o sistema de autorização for implementado. Regra futura: `AccountingAdmin` só poderá convidar `AccountingEmployee` da sua própria contabilidade; `SystemAdmin` poderá convidar qualquer role.

1. Valida se já existe um `UserInvite` com o mesmo email e `Status == Pending` — se sim, bloqueia com `Invite.AlreadyPending`
2. Valida se já existe um `User` com o mesmo email — se sim, bloqueia com `User.AlreadyExists`
3. Gera token cru — `Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))`
4. Cria `UserInvite` com `TokenHash = SHA256(token)`, `Status = Pending`, `ExpiresAt = UtcNow + 72h`
5. Persiste no banco
6. Monta `inviteLink = {frontend}/auth/accept-invite?token={tokenCru}`
7. `jobScheduler.Enqueue(new SendInviteEmailCommand(...))`
8. Retorna `IdentifierResponse(invite.Id)`

### Reenviar convite

1. Busca `UserInvite` pelo `Id` — não encontrado → `Invite.NotFound`
2. Valida se o solicitante tem permissão sobre esse convite (mesma regra do envio)
3. Valida se `Status == Pending` — se não → `Invite.NotResendable` (já foi aceito, expirado ou revogado)
4. Gera novo token cru
5. Chama `invite.Resend(newTokenHash, newExpiresAt)` — invalida token anterior, reinicia prazo de 72h
6. Persiste no banco
7. `jobScheduler.Enqueue(new SendInviteEmailCommand(...))`

### Aceitar convite

1. Busca `UserInvite` pelo `SHA256(request.Token)` — não encontrado → `Invite.NotFound`
2. Chama `invite.IsValid()` — falso → `Invite.Expired` ou `Invite.NotPending` conforme o caso
3. Valida `ConfirmPassword == Password`
4. Cria `User` com `FirstName`, `LastName`, `Email`, `Role`, `AccountancyId` herdados do `UserInvite` + `PasswordHash` da senha definida
5. Chama `invite.Accept()`
6. Persiste `User` e atualiza `UserInvite` no banco
7. Emite `UserRegistrationCompleted` com `InviteId` no payload

### Job de expiração

- Roda a cada hora via Hangfire `RecurringJob`
- Busca todos os `UserInvite` com `Status == Pending && ExpiresAt < UtcNow`
- Atualiza `Status = Expired` em lote
- Custo adicional de infra: nenhum — Hangfire já está na stack, job é uma query leve no PostgreSQL

---

## Casos de Erro

| Código | Quando ocorre |
|---|---|
| `Invite.AlreadyPending` | Já existe um convite `Pending` para o mesmo email |
| `Invite.NotFound` | `UserInvite` não encontrado pelo `Id` ou pelo hash do token |
| `Invite.NotResendable` | Tentativa de reenvio para convite que não está `Pending` |
| `Invite.NotRevokable` | Tentativa de revogação para convite que não está `Pending` |
| `Invite.Expired` | Token válido mas `ExpiresAt < UtcNow` |
| `Invite.NotPending` | Token válido mas `Status != Pending` (já aceito ou revogado) |
| `User.AlreadyExists` | Já existe um `User` com o mesmo email |

> ⚠️ Erros de autorização (ex: `Auth.Unauthorized`) serão adicionados quando o sistema de permissões for implementado.

---

## Estrutura de Arquivos

### O que muda

**`Identity.Domain/`**
- `Aggregates/User.cs` — remove `Status`, substitui `Name` por `FirstName` + `LastName`, `PasswordHash` passa a ser nullable até a aceitação
- `Enumerations/UserRole.cs` — migra de `enum` para `SmartEnum`
- `Enumerations/UserStatus.cs` — **removido** — substituído por `InviteStatus`

**`Identity.Application/`**
- `UseCases/Commands/InviteUserHandler.cs` — refatorado conforme novo fluxo
- `UseCases/Commands/AcceptInviteHandler.cs` — refatorado conforme novo fluxo
- `UseCases/Validators/InviteUserValidator.cs` — atualizado
- `UseCases/Validators/AcceptInviteValidator.cs` — atualizado
- `UseCases/Events/ProjectionUserEventHandler.cs` — atualizado para novos eventos

**`Identity.Shared/`**
- `Commands/InviteUserCommand.cs` — remove geração de senha, ajusta campos
- `Commands/AcceptInviteCommand.cs` — adiciona `FirstName` e `LastName`

---

### O que é criado

**`Identity.Domain/`**
```
Aggregates/
└── UserInvite.cs                      ← AggregateRoot com IsValid(), Accept(), Revoke(), Resend()
Enumerations/
└── InviteStatus.cs                    ← SmartEnum: Pending, Accepted, Expired, Revoked
Events/
└── DomainEvents.cs                    ← reestruturado com eventos de User e UserInvite
```

**`Identity.Application/`**
```
UseCases/
├── Commands/
│   ├── ResendInviteHandler.cs
│   └── RevokeInviteHandler.cs
├── Events/
│   └── ProjectionUserInviteEventHandler.cs
├── Jobs/
│   └── ExpireInvitesJob.cs            ← Hangfire RecurringJob — roda a cada hora
└── Validators/
    ├── ResendInviteValidator.cs
    └── RevokeInviteValidator.cs
```

**`Identity.Shared/`**
```
Commands/
├── ResendInviteCommand.cs
└── RevokeInviteCommand.cs
```

**`Identity.Persistence/`**
```
Configurations/
├── SnapshotConfiguration.cs       ← adiciona UserInviteSnapshotConfiguration
└── StoreEventConfiguration.cs    ← adiciona UserInviteStoreEventConfiguration
```

**`Web/Endpoints/Modules/Identity/`**
```
ResendInviteEndpoint.cs
RevokeInviteEndpoint.cs
Requests/Commands/
├── ResendInviteRequest.cs
└── RevokeInviteRequest.cs
```

---

## Prompt para o Claude Code

```
Vamos refatorar e expandir a funcionalidade de convite de usuários no módulo Identity.
Antes de qualquer implementação, leia os seguintes arquivos na íntegra:

1. Todo o módulo Identity — aggregates, entidades, eventos, handlers, validators, endpoints, persistência
2. O módulo WorkflowTemplate como referência de padrão — especialmente os aggregates, eventos e configurações de persistência
3. O arquivo invite-user-spec.md disponível no projeto

Só comece a implementar após confirmar que leu e entendeu todos os arquivos acima.

---

## O que implementar

### 1. Identity.Domain

**`Enumerations/UserRole.cs`** — migrar de enum para SmartEnum conforme especificação

**`Enumerations/InviteStatus.cs`** — novo SmartEnum: Pending, Accepted, Expired, Revoked

**`Enumerations/UserStatus.cs`** — remover

**`Aggregates/User.cs`**
- Remover propriedade `Status`
- Substituir `Name` por `FirstName` e `LastName`
- `PasswordHash` passa a ser nullable — só preenchido na aceitação do convite

**`Aggregates/UserInvite.cs`** — novo AggregateRoot
- Propriedades: Id, Email, Role, AccountancyId, TokenHash, Status, ExpiresAt, UsedAt
- Método público `bool IsValid()`
- Métodos públicos `Accept()` e `Revoke()` — validam Status == Pending antes de agir
- Método público `Resend(newTokenHash, newExpiresAt)` — valida Status == Pending, atualiza token e expiração
- Método privado `ChangeStatus(InviteStatus)` — atualiza Status e UsedAt = UtcNow
- Todos os métodos de mudança de estado devem emitir os eventos correspondentes via RaiseEvent

**`Events/DomainEvents.cs`** — reestruturar com:
- Eventos de User: UserRegistrationCompleted (carrega InviteId no payload)
- Eventos de UserInvite: UserInviteCreated, UserInviteResent, UserInviteAccepted, UserInviteRevoked, UserInviteExpired

---

### 2. Identity.Persistence

**`Configurations/SnapshotConfiguration.cs`** — adicionar UserInviteSnapshotConfiguration

**`Configurations/StoreEventConfiguration.cs`** — adicionar UserInviteStoreEventConfiguration

Gerar nova migration após as alterações de configuração.

---

### 3. Identity.Shared

**`Commands/InviteUserCommand.cs`** — ajustar: sem senha, sem campos desnecessários

**`Commands/AcceptInviteCommand.cs`** — adicionar FirstName e LastName

**`Commands/ResendInviteCommand.cs`** — novo: recebe InviteId (Guid)

**`Commands/RevokeInviteCommand.cs`** — novo: recebe InviteId (Guid)

---

### 4. Identity.Application

**`UseCases/Commands/InviteUserHandler.cs`** — refatorar:
1. Valida email duplicado em UserInvite com Status == Pending → Invite.AlreadyPending
2. Valida email duplicado em User → User.AlreadyExists
3. Gera token cru: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
4. Cria UserInvite com TokenHash = SHA256(token), Status = Pending, ExpiresAt = UtcNow + 72h
5. Persiste via AppendEventsAsync
6. Monta inviteLink = {FrontendUrl}/auth/accept-invite?token={tokenCru} (FrontendUrl vem do appsettings)
7. jobScheduler.Enqueue(new SendInviteEmailCommand(...))
8. Retorna IdentifierResponse(invite.Id)

**`UseCases/Commands/AcceptInviteHandler.cs`** — refatorar:
1. Busca UserInvite pelo SHA256(request.Token) → não encontrado: Invite.NotFound
2. invite.IsValid() → falso: Invite.Expired ou Invite.NotPending conforme o caso
3. Valida ConfirmPassword == Password
4. Cria User com FirstName, LastName, Email, Role, AccountancyId herdados do UserInvite + PasswordHash
5. invite.Accept()
6. Persiste User e UserInvite via AppendEventsAsync
7. UserRegistrationCompleted é emitido com InviteId no payload

**`UseCases/Commands/ResendInviteHandler.cs`** — novo:
1. Busca UserInvite pelo Id → não encontrado: Invite.NotFound
2. Valida Status == Pending → senão: Invite.NotResendable
3. Gera novo token cru
4. invite.Resend(newTokenHash, UtcNow + 72h)
5. Persiste via AppendEventsAsync
6. jobScheduler.Enqueue(new SendInviteEmailCommand(...))

**`UseCases/Commands/RevokeInviteHandler.cs`** — novo:
1. Busca UserInvite pelo Id → não encontrado: Invite.NotFound
2. invite.Revoke() → falha se Status != Pending: Invite.NotRevokable
3. Persiste via AppendEventsAsync

**`UseCases/Jobs/ExpireInvitesJob.cs`** — novo Hangfire RecurringJob:
- Busca todos UserInvite com Status == Pending e ExpiresAt < UtcNow
- Para cada um: chama método de expiração que emite UserInviteExpired
- Persiste via AppendEventsAsync
- Registrar como RecurringJob no startup com intervalo de 1 hora

**`UseCases/Validators/`** — atualizar InviteUserValidator e AcceptInviteValidator, criar ResendInviteValidator e RevokeInviteValidator

**`UseCases/Events/ProjectionUserEventHandler.cs`** — atualizar para novos eventos do User

**`UseCases/Events/ProjectionUserInviteEventHandler.cs`** — novo: projeções para os eventos do UserInvite

---

### 5. Web/Endpoints/Modules/Identity

**`ResendInviteEndpoint.cs`** — novo POST seguindo o padrão dos endpoints existentes

**`RevokeInviteEndpoint.cs`** — novo POST seguindo o padrão dos endpoints existentes

**`Requests/Commands/ResendInviteRequest.cs`** — novo

**`Requests/Commands/RevokeInviteRequest.cs`** — novo

---

## Restrições

- Seguir rigorosamente os padrões do projeto: IServiceInstaller, AppendEventsAsync, IJobSchedulerService para emails
- O token cru nunca deve ser armazenado — apenas o hash SHA256
- FrontendUrl deve vir do appsettings, nunca hardcoded
- Não implementar validação de permissões — será feita em etapa futura
- Usar WorkflowTemplate como referência de padrão para qualquer dúvida de implementação
- Apresentar o plano de alterações antes de executar e aguardar aprovação
```

