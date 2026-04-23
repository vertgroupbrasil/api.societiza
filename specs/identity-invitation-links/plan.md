# Plano — Identity Invitation Links

**Spec**: `specs/identity-invitation-links/spec.md` @ `vertgroupbrasil/product.societiza`
**Logic**: `specs/identity-invitation-links/logic.md` @ `vertgroupbrasil/product.societiza`
**Módulos afetados**: Identity (write + read), Accountancy (read via cross-module)
**Módulo novo?**: não
**Status**: **IMPLEMENTADO** — este documento descreve a implementação real do MVP.
**Resumo**: Implementa **links de convite** reutilizáveis (`InvitationLink` — aggregate no
Identity). Cada link carrega tipo (`TargetRole`), contabilidade alvo quando aplicável,
limite de usos e validade de 7 dias. O uso é consumido pelo cadastro do convidado
(`Register`) em operação atômica com incremento de contador; o link transita para
`Exhausted` quando `Uses == MaxUses`, ou `Expired` por tempo (via job recorrente).
**Revogação manual não foi implementada no MVP** — o link só encerra ciclo de vida
por expiração ou esgotamento de usos.

---

## Decisões de MVP

- **Sem revogação manual**: `InvitationLinkRevoked` event, `RevokeInvitationLinkCommand` e
  endpoint de revoke **não foram implementados**. Status terminal é apenas `Expired` ou
  `Exhausted`.
- **Token armazenado no aggregate e na projection** (não apenas o hash): simplifica a
  reconstrução de URL nos handlers de leitura sem roundtrip ao event store.
- **Consulta de usuários por link embutida no GetById**: não há endpoint separado —
  `GetInvitationLinkByIdResponse` já inclui a lista de usuários.
- **Sem endpoint público de validação de token**: não há `GetInvitationLinkByTokenEndpoint`
  ou `InvitationLinkPublicResponse`. O convidado só descobre se o link é válido ao tentar
  o cadastro.
- **Listagem paginada**: `ListInvitationLinksQuery` usa `IPagedResult<T>` com `PageNumber`
  e `PageSize`.
- **Sem índice parcial MongoDB de unicidade**: a garantia de "um link ativo por escopo"
  é feita via checagem soft no handler (query à projection antes de criar).

---

## Fases de implementação

### Fase 1 — Domain

**Referência**: `.claude/skills/architecture/references/domain-architecture.md`

#### 1. SmartEnum `InvitationLinkStatus`
- **Camada**: Domain
- **Tipo**: SmartEnum
- **Caminho**: `src/Modules/Identity/Identity.Domain/Enumerations/InvitationLinkStatus.cs`
- **Detalhes**: `Active = 0`, `Expired = 1`, `Exhausted = 3`. Sem `Revoked` no MVP.
  `class` com `[JsonConstructor]` ctor, implicit converters `string`/`int`/name/value,
  subclasses `public` por valor (padrão do projeto).

#### 2. Domain events da família `InvitationLink` em `DomainEvents.cs`
- **Camada**: Domain
- **Tipo**: DomainEvent
- **Caminho**: `src/Modules/Identity/Identity.Domain/Events/DomainEvents.cs`
- **Depende de**: #1
- **Detalhes**: 4 `sealed record`s na região `#region InvitationLink Events`:
  - `InvitationLinkCreated(Guid Id, string TargetRole, string Status, Guid? AccountancyId, Guid CreatedByUserId, string Token, string TokenHash, int MaxUses, DateTimeOffset ExpiresAt, DateTimeOffset CreatedAt, ulong Version)` — inclui **tanto `Token` quanto `TokenHash`**.
  - `InvitationLinkConsumed(Guid Id, int Uses, Guid ConsumedByUserId, DateTimeOffset ConsumedAt, DateTimeOffset UpdatedAt, ulong Version)`
  - `InvitationLinkExhausted(Guid Id, string Status, DateTimeOffset UpdatedAt, ulong Version)`
  - `InvitationLinkExpired(Guid Id, string Status, DateTimeOffset UpdatedAt, ulong Version)`
  - Todos: `sealed record ... : Message, IDomainEvent`. Tipos apenas primitivos.

#### 3. Aggregate `InvitationLink`
- **Camada**: Domain
- **Tipo**: Aggregate
- **Caminho**: `src/Modules/Identity/Identity.Domain/Aggregates/InvitationLink.cs`
- **Depende de**: #1, #2
- **Detalhes**: `public class InvitationLink : AggregateRoot`. Propriedades:
  `UserRole TargetRole`, `Guid? AccountancyId`, `Guid CreatedByUserId`, `string Token`,
  `string TokenHash`, `int MaxUses`, `int Uses`, `InvitationLinkStatus Status`,
  `DateTimeOffset ExpiresAt` (todas `private set`).
  - `public static InvitationLink Create(UserRole targetRole, Guid? accountancyId, Guid createdByUserId, string token, string tokenHash, int maxUses)` — levanta `InvitationLinkCreated` com `ExpiresAt = UtcNow + 7d`, `Status = Active`.
  - `public void Consume(Guid userId)` — **não valida estado internamente**; pré-condição é que o handler já chamou `IsUsable()`. Levanta `InvitationLinkConsumed(Uses + 1, userId, now)`. Se após o incremento `Uses == MaxUses`, levanta `InvitationLinkExhausted` em seguida.
  - `public void Expire()` — levanta `InvitationLinkExpired`.
  - `public bool IsUsable()` — `Status == Active && Uses < MaxUses && ExpiresAt > UtcNow`.
  - `protected override void ApplyEvent(IDomainEvent @event) => When(@event as dynamic)`.
  - `When(InvitationLinkCreated)` hidrata todas as propriedades (incluindo `Token` e `TokenHash`).
  - `When(InvitationLinkConsumed)` incrementa `Uses`, atualiza `UpdatedAt`.
  - `When(InvitationLinkExhausted)` seta `Status = Exhausted`, `UpdatedAt`.
  - `When(InvitationLinkExpired)` seta `Status = Expired`, `UpdatedAt`.

#### 4. Evento `UserRegistrationCompleted` — campo `InvitationLinkId`
- **Camada**: Domain
- **Tipo**: DomainEvent
- **Caminho**: `src/Modules/Identity/Identity.Domain/Events/DomainEvents.cs`
- **Detalhes**: `UserRegistrationCompleted` contém `Guid InvitationLinkId` (não-nullable).
  Todo cadastro novo vem obrigatoriamente de um link.

#### 5. Aggregate `User` — `Create()` com `invitationLinkId`
- **Camada**: Domain
- **Tipo**: Aggregate (modificação)
- **Caminho**: `src/Modules/Identity/Identity.Domain/Aggregates/User.cs`
- **Depende de**: #4
- **Detalhes**: `public static User Create(Guid invitationLinkId, string firstName, string lastName, string email, string passwordHash, UserRole role, Guid? accountancyId)`. Levanta `UserRegistrationCompleted` com `InvitationLinkId = invitationLinkId`.

#### 6. Projection model `InvitationLink`
- **Camada**: Domain
- **Tipo**: ProjectionModel
- **Caminho**: `src/Modules/Identity/Identity.Domain/Projection/ProjectionModel.cs`
- **Depende de**: #1
- **Detalhes**: `record InvitationLink(Guid Id, string TargetRole, string Status, Guid? AccountancyId, Guid CreatedByUserId, string Token, string TokenHash, int MaxUses, int Uses, DateTimeOffset ExpiresAt, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt, bool IsDeleted = false) : IProjectionModel`. Armazena **tanto `Token` quanto `TokenHash`** para permitir reconstrução de URL nos handlers de leitura.

#### 7. Projection model `User` — campo `InvitationLinkId`
- **Camada**: Domain
- **Tipo**: ProjectionModel (modificação)
- **Caminho**: `src/Modules/Identity/Identity.Domain/Projection/ProjectionModel.cs`
- **Depende de**: #4
- **Detalhes**: `Guid? InvitationLinkId = null` adicionado ao record `User`. Nullable para compatibilidade com registros legados (se houver).

**Build gate**: `dotnet build Societiza.sln` — zero errors antes de avançar.

---

### Fase 2 — Shared

**Referência**: `.claude/skills/architecture/references/shared-architecture.md`

> Pasta: `Identity.Shared/Commands/Invitation/` e `Identity.Shared/Queries/Invitation/`
> (singular, não plural).

#### 1. Command `CreateInvitationLinkCommand`
- **Caminho**: `src/Modules/Identity/Identity.Shared/Commands/Invitation/CreateInvitationLinkCommand.cs`
- **Detalhes**: `sealed record CreateInvitationLinkCommand(string TargetRole, Guid? AccountancyId, int? MaxUses) : ICommand<CreateInvitationLinkResponse>`.

#### 2. Response DTO `CreateInvitationLinkResponse`
- **Caminho**: `src/Modules/Identity/Identity.Shared/Response/Invitation/CreateInvitationLinkResponse.cs`
- **Detalhes**: `sealed record CreateInvitationLinkResponse(Guid Id, string Token, string Url, DateTimeOffset ExpiresAt)`.

#### 3. Command `RegisterUserCommand`
- **Caminho**: `src/Modules/Identity/Identity.Shared/Commands/User/RegisterUserCommand.cs`
- **Detalhes**: `sealed record RegisterUserCommand(string Token, string FirstName, string LastName, string Email, string Password, string ConfirmPassword) : ICommand<IdentifierResponse>`.

#### 4. Command `ExpireInvitationLinksCommand`
- **Caminho**: `src/Modules/Identity/Identity.Shared/Jobs/ExpireInvitationLinksCommand.cs`
- **Detalhes**: `sealed record ExpireInvitationLinksCommand() : ICommand`. Comando de job, sem parâmetros.

#### 5. Query `GetInvitationLinkByIdQuery`
- **Caminho**: `src/Modules/Identity/Identity.Shared/Queries/Invitation/GetInvitationLinkByIdQuery.cs`
- **Detalhes**: `sealed record GetInvitationLinkByIdQuery(Guid InvitationLinkId) : IQuery<InvitationLinkByIdResponse>`. Retorna dados completos do link + lista de usuários registrados.

#### 6. Response DTO `InvitationLinkByIdResponse`
- **Caminho**: `src/Modules/Identity/Identity.Shared/Response/Invitation/InvitationLinkByIdResponse.cs`
- **Detalhes**: `sealed record InvitationLinkByIdResponse(Guid Id, string TargetRole, string Status, Guid? AccountancyId, Guid CreatedByUserId, int MaxUses, int Uses, DateTimeOffset ExpiresAt, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt, string Url, List<InvitationLinkUserResponse> Users)`. Inclui `Url` e lista embutida de usuários.

#### 7. Query `ListInvitationLinksQuery`
- **Caminho**: `src/Modules/Identity/Identity.Shared/Queries/Invitation/ListInvitationLinksQuery.cs`
- **Detalhes**: `sealed record ListInvitationLinksQuery(bool OnlyActive, Guid? AccountancyId, int PageNumber, int PageSize) : IQuery<IPagedResult<InvitationLinkDetailResponse>>`. Paginado.

#### 8. Response DTO `InvitationLinkDetailResponse`
- **Caminho**: `src/Modules/Identity/Identity.Shared/Response/Invitation/InvitationLinkDetailResponse.cs`
- **Detalhes**: `sealed record InvitationLinkDetailResponse(Guid Id, string TargetRole, string Status, Guid? AccountancyId, Guid CreatedByUserId, int MaxUses, int Uses, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, DateTimeOffset? UpdatedAt, string Url)`. Inclui `Url` construída a partir do `Token` da projection.

#### 9. Response DTO `InvitationLinkUserResponse`
- **Caminho**: `src/Modules/Identity/Identity.Shared/Response/Invitation/InvitationLinkUserResponse.cs`
- **Detalhes**: `sealed record InvitationLinkUserResponse(Guid UserId, string FirstName, string LastName, string Email, DateTimeOffset JoinedAt)`.

**Build gate**: `dotnet build Societiza.sln` — zero errors antes de avançar.

---

### Fase 3 — Application

**Referência**: `.claire/skills/architecture/references/application-architecture.md`

#### 1. Interface cross-module `IAccountancyDataService`
- **Caminho**: `src/Modules/Identity/Identity.Application/Services/IAccountancyDataService.cs`
- **Detalhes**: `public interface IAccountancyDataService { Task<bool> ExistsAsync(Guid accountancyId, CancellationToken ct); }`. Implementação em `src/Web/Services/CrossModule/AccountancyDataService.cs` (Fase 6).

#### 2. Validator `CreateInvitationLinkValidator`
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Validators/Invitation/CreateInvitationLinkValidator.cs`
- **Detalhes**:
  - `TargetRole` NotEmpty + `UserRole.TryFromName` válido.
  - `AccountancyId` se presente: `!= Guid.Empty`.
  - `MaxUses` quando `TargetRole == AccountancyEmployee`: NotNull + `>= 1`.
  - `MaxUses` quando `TargetRole ∈ {SystemAdmin, AccountancyAdmin}`: deve ser Null — limite fixo a 1.

#### 3. Handler `CreateInvitationLinkHandler`
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Commands/Invitation/CreateInvitationLinkHandler.cs`
- **Injeções**: `IIdentityApplicationService`, `IIdentityProjection<ProjectionModel.InvitationLink>`, `IAccountancyDataService`, `ICurrentUserService`, `IOptions<FrontendOptions>`.
- **Ordem de validação no handler**:
  1. Parse de `callerRole` via `UserRole.TryFromName(currentUser.Role)`.
  2. **Matriz de autorização**: SA → {SA, AA}; AA → {AA, AE}; senão `ForbiddenError`.
  3. **Resolução de `accountancyId`**: SA→SA: null (rejeita se vier valor); SA→AA: obrigatório + `ExistsAsync`; AA→qualquer: força `currentUser.AccountancyId`.
  4. **Limite de usos**: SA/AA: fixo `maxUses = 1`; AE: `request.MaxUses!.Value`.
  5. **Unicidade de escopo ativo** via projection (soft check): `ConflictError` se existir link ativo no escopo.
  6. **Geração do token**: `rawToken = RandomNumberGenerator.GetBytes(32) → Base64`; `tokenHash = SHA256 → Base64`.
  7. `InvitationLink.Create(targetRole, accountancyId, callerId, rawToken, tokenHash, maxUses)` + `AppendEventsAsync`.
  8. Retorno: `CreateInvitationLinkResponse(link.Id, rawToken, $"{baseUrl}/auth/join?token={rawToken}", link.ExpiresAt)`.

#### 4. Validator `RegisterUserValidator`
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Validators/User/RegisterUserValidator.cs`
- **Detalhes**: `Token` NotEmpty; `FirstName/LastName` NotEmpty + MaxLength(100); `Email` NotEmpty + EmailAddress + MaxLength(150); `Password` NotEmpty + MinLength(8) + MaxLength(100) + regex uppercase + lowercase + digit; `ConfirmPassword Equal(x => x.Password)`.

#### 5. Handler `RegisterUserHandler`
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Commands/User/RegisterUserHandler.cs`
- **Injeções**: `IIdentityApplicationService`, `IIdentityProjection<ProjectionModel.InvitationLink>`, `IIdentityProjection<ProjectionModel.User>`, `IPasswordService`.
- **Fluxo**:
  1. Hash do token → `linkProjection.FindAsync(x => x.TokenHash == tokenHash && !x.IsDeleted)`. Se não → `NotFoundError`.
  2. `userProjection.FindAsync(x => x.Email == request.Email)`. Se existe → `ConflictError("User.AlreadyExists")`.
  3. `LoadAggregateAsync<InvitationLink>(linkSnapshot.Id)` — fonte de verdade.
  4. `link.IsUsable()` → se false, retorna erro com motivo (`Expired`/`Exhausted`/`NotUsable`).
  5. Gera `userId = Guid.NewGuid()`, `passwordHash = passwordService.HashPassword(...)`.
  6. `link.Consume(userId)` — void, sem retorno de Result.
  7. `UserAggregate.Create(link.Id, firstName, lastName, email, passwordHash, link.TargetRole, link.AccountancyId)`.
  8. `AppendEventsAsync(user)` depois `AppendEventsAsync(link)` — duas transações sequenciais.
  9. Retorna `IdentifierResponse(user.Id)`.

#### 6. Query handler `GetInvitationLinkByIdHandler`
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Queries/Invitation/GetInvitationLinkByIdHandler.cs`
- **Injeções**: `IIdentityProjection<ProjectionModel.InvitationLink>`, `IIdentityProjection<ProjectionModel.User>`, `ICurrentUserService`, `IOptions<FrontendOptions>`.
- **Fluxo**: parse callerRole; busca link por Id; verifica acesso (SA: qualquer; AA: só `link.AccountancyId == callerAccountancyId`); `userProjection.ListAsync(x => x.InvitationLinkId == link.Id && !x.IsDeleted)` ordenados por `CreatedAt`; constrói `Url = $"{baseUrl}/auth/join?token={link.Token}"`; retorna `InvitationLinkByIdResponse` com lista embutida.

#### 7. Query handler `ListInvitationLinksHandler`
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Queries/Invitation/ListInvitationLinksHandler.cs`
- **Injeções**: `IIdentityProjection<ProjectionModel.InvitationLink>`, `ICurrentUserService`, `IOptions<FrontendOptions>`.
- **Fluxo**:
  - AA: força `accountancyId = callerAccountancyId`.
  - SA sem `AccountancyId`: somente links `TargetRole == SystemAdmin`.
  - SA com `AccountancyId`: todos os links daquela contabilidade.
  - Demais roles: `ForbiddenError`.
  - Filtro `OnlyActive`: `Status == Active && ExpiresAt > now` (calculado em tempo real).
  - Ordenação: ativo DESC, `CreatedAt` DESC. Url construída a partir de `x.Token`.
  - Retorna `PagedResult<InvitationLinkDetailResponse>.Create(paging, ordered)`.

#### 8. Projection handler `ProjectionInvitationLinkEventHandler`
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Events/ProjectionInvitationLinkEventHandler.cs`
- **Detalhes**: composite interface `IProjectionInvitationLinkEventHandler : IEventHandler<Created>, IEventHandler<Consumed>, IEventHandler<Exhausted>, IEventHandler<Expired>` (4 eventos — sem Revoked).
  - `Created`: `ReplaceInsertAsync(new ProjectionModel.InvitationLink(...))` com `Uses = 0`.
  - `Consumed`: `FindAsync` → `existing with { Uses, UpdatedAt }` → upsert. Log warning se null (não relança).
  - `Exhausted`/`Expired`: `existing with { Status, UpdatedAt }`.
  - Todos: try/catch + `throw new InvalidOperationException(...)` para retry MassTransit.

#### 9. Projection handler `ProjectionUserEventHandler` — propaga `InvitationLinkId`
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Events/ProjectionUserEventHandler.cs`
- **Detalhes**: `Handle(UserRegistrationCompleted)` passa `@event.InvitationLinkId` ao construir `ProjectionModel.User`.

#### 10. Handler `ExpireInvitationLinksHandler`
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Jobs/ExpireInvitationLinksJob.cs`
- **Detalhes**: `ICommandHandler<ExpireInvitationLinksCommand>`. `linkProjection.ListAsync(x => x.Status == Active && x.ExpiresAt < now)` → `foreach` sequencial (não `Parallel.ForEachAsync`) → `LoadAggregateAsync` → `link.Expire()` → `AppendEventsAsync`. Log warning se load falhar; continua processamento.

#### 11. Registrar projection handlers em `AddEventInteractors()`
- **Caminho**: `src/Modules/Identity/Identity.Application/DependencyInjection/ServiceCollectionExtensions.cs`
- **Detalhes**: `.AddScoped<IProjectionInvitationLinkEventHandler, ProjectionInvitationLinkEventHandler>()` adicionado junto aos demais.

**Build gate**: `dotnet build Societiza.sln` — zero errors antes de avançar.

---

### Fase 4 — Persistence

**Referência**: `.claude/skills/architecture/references/persistence-architecture.md`

#### 1. EF configuration `InvitationLinkStoreEventConfiguration`
- **Caminho**: `src/Modules/Identity/Identity.Persistence/Configurations/StoreEventConfiguration.cs`
- **Detalhes**: classe interna `InvitationLinkStoreEventConfiguration : StoreEventConfiguration<InvitationLink>` (vazia, herda tudo do Core).

#### 2. EF configuration `InvitationLinkSnapshotConfiguration`
- **Caminho**: `src/Modules/Identity/Identity.Persistence/Configurations/SnapshotConfiguration.cs`
- **Detalhes**: classe interna `InvitationLinkSnapshotConfiguration : SnapshotConfiguration<InvitationLink>` (vazia).

#### 3. Migration EF
- **Caminho**: `src/Modules/Identity/Identity.Persistence/Migrations/`
- **Detalhes**: migration `InitialMigrate` (primeira migration do módulo Identity) já inclui as tabelas `identity."InvitationLinkStoreEvents"` e `identity."InvitationLinkSnapshots"` junto com as tabelas de `User`. Não há migration separada apenas para `InvitationLink`.

#### 4. `IdentityDbContext` aplica configs do assembly
- **Caminho**: `src/Modules/Identity/Identity.Persistence/Context/IdentityDbContext.cs`
- **Detalhes**: `modelBuilder.ApplyConfigurationsFromAssembly(AssemblyReference.Assembly)` já está em `OnModelCreating` — as classes aninhadas são descobertas automaticamente. **Sem alteração.**

> **Nota**: índice parcial único MongoDB (unicidade por escopo) **não foi implementado no MVP**.
> A garantia de "um link ativo por escopo" é feita exclusivamente via checagem soft no handler
> (`FindAsync` antes de criar). Race conditions muito improváveis no volume atual são aceitas
> como risco de MVP.

**Build gate**: `dotnet build Societiza.sln` — zero errors antes de avançar.

---

### Fase 5 — Infrastructure

Nenhuma alteração. `AppendEventsAsync<InvitationLink>` resolve `StoreEvent<InvitationLink>`
via DbSet dinâmico — já suportado pelo `ApplicationService` existente. A classe
`IdentityApplicationService` permanece intacta.

**Build gate**: `dotnet build Societiza.sln` — zero errors antes de avançar.

---

### Fase 6 — Web

**Referência**: `.claude/skills/architecture/references/web-architecture.md` + skill `/api-design`

#### 1. Rotas em `IdentityRoutes`
- **Caminho**: `src/Web/Endpoints/Routes/IdentityRoutes.cs`
- **Detalhes**: 4 rotas de invitation link:
  ```csharp
  internal const string InvitationLinkId = "invitationLinkId";

  internal const string CreateInvitationLink  = $"{BaseUri}/invitation-link";
  internal const string ListInvitationLinks   = $"{BaseUri}/invitation-link";
  internal const string GetInvitationLinkById = $"{BaseUri}/invitation-link/{{{InvitationLinkId}:guid}}";
  internal const string Register              = $"{BaseUri}/invitation-link/register";
  ```

#### 2. Implementação cross-module `AccountancyDataService`
- **Caminho**: `src/Web/Services/CrossModule/AccountancyDataService.cs`
- **Detalhes**: `public sealed class AccountancyDataService(ISender sender) : IAccountancyDataService` com `ExistsAsync(id, ct) => (await sender.Send(new GetAccountancyDetailByIdQuery(id))).IsSuccess`.

#### 3. Registrar `IAccountancyDataService` em `CrossModuleServicesInstaller`
- **Caminho**: `src/Web/ServiceInstallers/CrossModule/CrossModuleServicesInstaller.cs`
- **Detalhes**: `services.AddScoped<IAccountancyDataService, AccountancyDataService>()`.

#### 4. Requests (pasta `Invitation/`, singular)
- `CreateInvitationLinkRequest` — `string TargetRole`, `Guid? AccountancyId`, `int? MaxUses`
- `GetInvitationLinkByIdRequest` — `[FromRoute] Guid InvitationLinkId`
- `ListInvitationLinksRequest` — `[FromQuery] bool OnlyActive = true`, `Guid? AccountancyId`, `int PageNumber = 1`, `int PageSize = 10`
- `RegisterUserRequest` (pasta `User/`) — `string Token, Email, FirstName, LastName, Password, ConfirmPassword`

#### 5. Endpoints (4 endpoints)

| Endpoint | Rota | Verb | Authorize | Tag |
|---|---|---|---|---|
| `CreateInvitationLinkEndpoint` | `CreateInvitationLink` | POST | `CanManageInvites` | `Identity.Invites` |
| `GetInvitationLinkByIdEndpoint` | `GetInvitationLinkById` | GET | `CanManageInvites` | `Identity.Invites` |
| `ListInvitationLinksEndpoint` | `ListInvitationLinks` | GET | `CanManageInvites` | `Identity.Invites` |
| `RegisterUserEndpoint` | `Register` | POST | **público** | `Identity.User` |

Status codes por endpoint:
- Create: `200`, `400`, `401`, `403`, `409`
- GetById: `200`, `400`, `401`, `403`, `404`
- List: `200`, `400`, `401`, `403`
- Register: `200`, `400`, `404`, `409`

#### 6. MassTransit consumer `ProjectionInvitationLinkConsumer`
- **Caminho**: `src/Web/ServiceInstallers/EventBus/Consumers/IdentityModule/ProjectionInvitationLinkConsumer.cs`
- **Detalhes**: `IConsumer<InvitationLinkCreated>, IConsumer<InvitationLinkConsumed>, IConsumer<InvitationLinkExhausted>, IConsumer<InvitationLinkExpired>` (4 eventos).

#### 7. `ConsumerConfiguration` — 4 endpoints para InvitationLink
- **Caminho**: `src/Web/ServiceInstallers/EventBus/Consumers/IdentityModule/ConsumerConfiguration.cs`
- **Detalhes**: 4 chamadas `ConfigureEventReceiveEndpoint` para os eventos `Created`, `Consumed`, `Exhausted`, `Expired`.

#### 8. Job recorrente `ExpireInvitationLinks`
- **Caminho**: `src/Web/StartupTasks/RegisterRecurringJobsStartupTask.cs`
- **Detalhes**: `jobScheduler.ScheduleRecurring("expire-pending-invitation-links", "0 * * * *", new ExpireInvitationLinksCommand())` — executa a cada hora.

**Build gate**: `dotnet build Societiza.sln` — zero errors. Depois: `dotnet test Societiza.sln` — todos passando.

---

## Checklist de registro

- [x] **`ConsumerConfiguration.cs`** — 4 chamadas de `ConfigureEventReceiveEndpoint` para eventos do `InvitationLink`.
- [x] **`AddEventInteractors()`** — `AddScoped<IProjectionInvitationLinkEventHandler, ProjectionInvitationLinkEventHandler>()`.
- [x] **`MigrateDatabaseStartupTask`** — sem alteração. `IdentityDbContext` já estava registrado; `InitialMigrate` cobre as tabelas do aggregate.
- [x] **`RegisterRecurringJobsStartupTask`** — job `"expire-pending-invitation-links"` adicionado.
- [x] **`PersistenceServiceInstaller`** — sem alteração (`IIdentityProjection<>` já registrado cobre `ProjectionModel.InvitationLink`).
- [x] **`Web.csproj`** — sem alteração.
- [x] **`CrossModuleServicesInstaller`** — `AddScoped<IAccountancyDataService, AccountancyDataService>()`.
- [ ] **`StartupTasksServiceInstaller`** — sem `EnsureProjectionIndexesStartupTask` (índice MongoDB não implementado no MVP).

---

## O que NÃO foi implementado (fora do escopo do MVP)

| Item planejado | Decisão |
|---|---|
| `InvitationLinkRevoked` domain event | Não implementado — sem revogação manual no MVP |
| `RevokeInvitationLinkCommand` / handler / validator / endpoint | Não implementado |
| `InvitationLinkStatus.Revoked` (valor 3) | Não existe; `Exhausted` tem valor 3 |
| `GetInvitationLinkByTokenQuery` / `InvitationLinkPublicResponse` | Não implementado — validação só ocorre no Register |
| `ListUsersByInvitationLinkQuery` / endpoint separado | Não implementado — usuários embutidos no GetById |
| Índice parcial MongoDB de unicidade por escopo | Não implementado — soft check no handler |
| `EnsureProjectionIndexesStartupTask` | Não implementado |
| `Parallel.ForEachAsync` no job de expiração | Substituído por foreach sequencial |

---

## Riscos aceitos no MVP

1. **Sem revogação manual**: links `SA→AA` criados sem um AA no tenant ficam sem ator capaz de encerrá-los antes da expiração natural (7 dias). Aceito para o MVP.

2. **Dupla transação no registro** — `AppendEventsAsync(user)` + `AppendEventsAsync(link)` são transações separadas. Se a segunda falhar, o user existe mas o contador do link não foi incrementado. O convidado não consegue novo cadastro (email duplicado). Aceito para o MVP.

3. **Race condition na unicidade de escopo** — checagem soft + projection eventually consistent: há janela de ms onde dois requests simultâneos podem criar links no mesmo escopo. Aceito para o MVP (volume baixo).

4. **Token no path/response** — o raw token é retornado no `CreateInvitationLinkResponse` e armazenado na projection (para construção de URL). Aceito para o MVP; em produção avaliar mover para one-time retrieval.