# Plano — Identity User Management

**Spec**:  `specs/identity-user-management/spec.md` @ `vertgroupbrasil/product.societiza`
**Logic**: `specs/identity-user-management/logic.md` @ `vertgroupbrasil/product.societiza`
**Módulos afetados**: Identity (write + read)
**Módulo novo?**: não
**Resumo**: Estende o aggregate `User` existente com (i) edição do próprio nome,
(ii) promoção `AccountancyEmployee → AccountancyAdmin`, e (iii) soft-delete (deletion)
com invalidação imediata de sessão. Adiciona endpoints de leitura (perfil próprio,
SystemAdmins, membros de uma contabilidade). Sem novos aggregates, sem novos módulos.

---

## Pré-condições

- Aggregate `User` existente em `src/Modules/Identity/Identity.Domain/Aggregates/User.cs`
  precisa receber 3 eventos novos. O soft-delete já existe via flag `IsDeleted` na
  base `Entity` — não serão adicionados campos próprios de status ao aggregate.
  A projection `ProjectionModel.User` já carrega `IsDeleted` herdado de
  `IProjectionModel`; não há backfill necessário.
- Decisão da sessão registrada: **não há endpoints de listagem de usuários deletados** —
  apesar de a spec/logic mencionarem "visão separada de inativos", esse caminho está
  intencionalmente fora do escopo deste plano. Filtros sempre `!IsDeleted`.
- `IdentityPolicies.CanPromoteUser` e `IdentityPolicies.CanManageUsers` já existem
  e são reutilizados — sem novas policies neste plano.
- `ICurrentUserService` (Core) já expõe `UserId`, `Email`, `Role`, `AccountancyId`
  e `ResolveAccountancyId(routeAccountancyId)` — usado nos handlers para amarrar
  fronteira multi-tenant.

---

## Fases de implementação

### Fase 1 — Domain

**Referência**: `.claude/skills/architecture/references/domain-architecture.md`

#### 1. Domain event `UserNameUpdated`
- **Camada**: Domain
- **Tipo**: DomainEvent
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Domain/Events/DomainEvents.cs`
- **Depende de**: nenhum
- **Spec-ref**: spec §8 "Edição do próprio perfil"; logic §3 "Todo usuário pode editar apenas o próprio nome"
- **Detalhes**: `sealed record UserNameUpdated(Guid UserId, string FirstName, string LastName, DateTimeOffset UpdatedAt, ulong Version) : Message, IDomainEvent`. Vai dentro da `#region User Events`.

#### 2. Domain event `UserPromotedToAccountancyAdmin`
- **Camada**: Domain
- **Tipo**: DomainEvent
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Domain/Events/DomainEvents.cs`
- **Depende de**: nenhum
- **Spec-ref**: spec §8 "Promoção AE → AA"; logic §2 "AccountancyEmployee → AccountancyAdmin"; logic §3 "AccountancyAdmin pode promover apenas AccountancyEmployee"
- **Detalhes**: `sealed record UserPromotedToAccountancyAdmin(Guid UserId, Guid AccountancyId, string Role, Guid PromotedBy, DateTimeOffset PromotedAt, ulong Version) : Message, IDomainEvent`. `Role` é o **novo** perfil em `string` (`UserRole.AccountancyAdmin.Name`).

#### 3. Domain event `UserDeleted`
- **Camada**: Domain
- **Tipo**: DomainEvent
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Domain/Events/DomainEvents.cs`
- **Depende de**: nenhum
- **Spec-ref**: spec §8 "Soft delete"; logic §2 "Ativo → Inativo"; logic §3 "Sessão do usuário removido é invalidada imediatamente"
- **Detalhes**: `sealed record UserDeleted(Guid UserId, Guid? AccountancyId, Guid DeletedBy, DateTimeOffset DeletedAt, ulong Version) : Message, IDomainEvent`. `AccountancyId` é nullable porque SystemAdmins não têm contabilidade.

#### 4. Aggregate `User` — métodos novos
- **Camada**: Domain
- **Tipo**: Aggregate (modificação)
- **Ação**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Domain/Aggregates/User.cs`
- **Depende de**: #1, #2, #3
- **Spec-ref**: spec §8 (linhas: edição de nome, promoção, soft-delete, sessão); logic §2 e §3
- **Detalhes**:
  - **Sem campos novos no aggregate** — soft-delete usa a flag `IsDeleted` herdada da base `Entity`.
  - Método `public void UpdateName(string firstName, string lastName)` — levanta `UserNameUpdated` via `RaiseEvent<T>()`. **Sem validação interna** (validação no Validator/Handler).
  - Método `public void PromoteToAccountancyAdmin(Guid promotedBy)` — levanta `UserPromotedToAccountancyAdmin` com `Role: UserRole.AccountancyAdmin.Name`, `AccountancyId: AccountancyId.Value`. **Sem validação de estado interna** — handler já checou que é `AccountancyEmployee`, que `AccountancyId` está setado e que solicitante é AA do mesmo tenant.
  - Método `public void Delete(Guid deletedBy)` — levanta `UserDeleted`. Como efeito colateral: também chama `RevokeAllRefreshTokens()` (já existente) e `RegenerateSecurityStamp()` (já existente) **dentro** do mesmo método, garantindo invalidação de sessão atômica.
  - `When(UserNameUpdated)` → `FirstName = @event.FirstName; LastName = @event.LastName; UpdatedAt = @event.UpdatedAt;`.
  - `When(UserPromotedToAccountancyAdmin)` → `Role = UserRole.FromName(@event.Role); UpdatedAt = @event.PromotedAt;`.
  - `When(UserDeleted)` → seta `IsDeleted = true` (herdado da base `Entity`) e `UpdatedAt = @event.DeletedAt;`. A flag `IsDeleted` é usada por `LoadAggregateAsync` para falhar com `ConflictError` em operações futuras sobre o aggregate deletado — comportamento desejado para soft-delete.

#### 5. Projection model `User` — sem campos novos
- **Camada**: Domain
- **Tipo**: ProjectionModel (sem modificação estrutural)
- **Ação**: nenhuma
- **Caminho**: `src/Modules/Identity/Identity.Domain/Projection/ProjectionModel.cs`
- **Depende de**: nenhum
- **Spec-ref**: spec §8 "Soft delete"; logic §3 "Listagem de membros ativos é a visão padrão"
- **Detalhes**: O record já possui a flag `IsDeleted` herdada de `IProjectionModel`. Listagens de membros ativos filtram por `!IsDeleted`. Nenhum campo novo é adicionado ao record.

**Build gate**: `dotnet build Societiza.sln` — zero errors antes de avançar.

---

### Fase 2 — Shared

**Referência**: `.claude/skills/architecture/references/shared-architecture.md`

#### 1. Command `UpdateUserNameCommand`
- **Camada**: Shared
- **Tipo**: Command
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Commands/User/UpdateUserNameCommand.cs`
- **Depende de**: nenhum
- **Spec-ref**: spec §8 "Edição do próprio perfil"; logic §3 "Todo usuário pode editar apenas o próprio nome"
- **Detalhes**: `sealed record UpdateUserNameCommand(string FirstName, string LastName) : ICommand`. **Sem `UserId`** — handler usa `ICurrentUserService.UserId` (regra: cross-user é rejeitado, então o command nem expõe o alvo).

#### 2. Command `PromoteUserToAccountancyAdminCommand`
- **Camada**: Shared
- **Tipo**: Command
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Commands/User/PromoteUserToAccountancyAdminCommand.cs`
- **Depende de**: nenhum
- **Spec-ref**: spec §8 "Promoção AE → AA"; logic §1 (fluxo AccountancyAdmin → Promover)
- **Detalhes**: `sealed record PromoteUserToAccountancyAdminCommand(Guid AccountancyId, Guid UserId) : ICommand<UserDetailResponse>`. Retorna o perfil atualizado (logic §4: "resposta de sucesso deve incluir o novo perfil"). `AccountancyId` vem da rota.

#### 3. Command `DeleteUserCommand`
- **Camada**: Shared
- **Tipo**: Command
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Commands/User/DeleteUserCommand.cs`
- **Depende de**: nenhum
- **Spec-ref**: spec §8 "Exclusão por SystemAdmin/AccountancyAdmin"; logic §1 (fluxos de remoção); logic §3 (regras de bloqueio)
- **Detalhes**: `sealed record DeleteUserCommand(Guid UserId, Guid? AccountancyId) : ICommand`. `AccountancyId` opcional (preenchido só por SystemAdmin via query param; AccountancyAdmin tem o tenant resolvido via claim).

#### 4. Response DTO `UserProfileResponse`
- **Camada**: Shared
- **Tipo**: ResponseDTO
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Response/User/UserProfileResponse.cs`
- **Depende de**: nenhum
- **Spec-ref**: spec §1 "Qualquer usuário encontra seu próprio perfil"; logic §4 "O nome atualizado do usuário autenticado após edição"
- **Detalhes**: `sealed record UserProfileResponse(Guid Id, string FirstName, string LastName, string Email, string Role, Guid? AccountancyId, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt)`. Usado no `GET /users` (perfil próprio).

#### 5. Response DTO `UserDetailResponse`
- **Camada**: Shared
- **Tipo**: ResponseDTO
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Response/User/UserDetailResponse.cs`
- **Depende de**: nenhum
- **Spec-ref**: spec §8 "Dados visíveis na listagem — AccountancyAdmin e SystemAdmin"
- **Detalhes**: `sealed record UserDetailResponse(Guid Id, string FirstName, string LastName, string Email, string Role, DateTimeOffset JoinedAt, Guid? InvitationLinkId)`. Usado nas listagens de SystemAdmins e Members (visão completa para admins). `JoinedAt` mapeia `CreatedAt` da projection.

#### 6. Response DTO `UserSummaryResponse`
- **Camada**: Shared
- **Tipo**: ResponseDTO
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Response/User/UserSummaryResponse.cs`
- **Depende de**: nenhum
- **Spec-ref**: spec §8 "Dados visíveis na listagem — AccountancyEmployee"; logic §3 "AccountancyEmployee vê apenas nome e perfil dos colegas"
- **Detalhes**: `sealed record UserSummaryResponse(Guid Id, string FirstName, string LastName, string Role)`. Usado quando o solicitante é AccountancyEmployee.

#### 7. Query `GetMyProfileQuery`
- **Camada**: Shared
- **Tipo**: Query
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Queries/User/GetMyProfileQuery.cs`
- **Depende de**: nenhum
- **Spec-ref**: spec §1 "Qualquer usuário encontra seu próprio perfil"
- **Detalhes**: `sealed record GetMyProfileQuery() : IQuery<UserProfileResponse>`. Sem campos — handler usa `ICurrentUserService.UserId`.

#### 8. Query `ListSystemAdminsQuery`
- **Camada**: Shared
- **Tipo**: Query
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Queries/User/ListSystemAdminsQuery.cs`
- **Depende de**: nenhum
- **Spec-ref**: spec §1 "lista de outros SystemAdmins em sua área administrativa"; logic §1 SystemAdmin "Visualiza a lista de todos os SystemAdmins ativos"
- **Detalhes**: `sealed record ListSystemAdminsQuery() : IQuery<IReadOnlyList<UserDetailResponse>>`. Sem paginação no MVP — número de SystemAdmins é pequeno por natureza.

#### 9. Query `ListAccountancyMembersQuery`
- **Camada**: Shared
- **Tipo**: Query
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Queries/User/ListAccountancyMembersQuery.cs`
- **Depende de**: nenhum
- **Spec-ref**: spec §1 "área de Equipe"; logic §1 todos os fluxos de Listagem
- **Detalhes**: `sealed record ListAccountancyMembersQuery(Guid AccountancyId) : IQuery<AccountancyMembersResponse>`. Resposta heterogênea (admin recebe `UserDetailResponse[]`, employee recebe `UserSummaryResponse[]`) — ver #10.

#### 10. Response DTO `AccountancyMembersResponse`
- **Camada**: Shared
- **Tipo**: ResponseDTO
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Response/User/AccountancyMembersResponse.cs`
- **Depende de**: #5, #6
- **Spec-ref**: spec §8 "Dados visíveis na listagem"; logic §3 e §5 (filtragem no backend baseada no perfil do solicitante)
- **Detalhes**: `sealed record AccountancyMembersResponse(IReadOnlyList<UserDetailResponse>? Members, IReadOnlyList<UserSummaryResponse>? Summaries)`. Apenas um dos dois campos é preenchido pelo handler conforme o perfil do solicitante. Alternativa considerada (e descartada): retornar dois endpoints distintos. Manter um único endpoint mantém a contratação alinhada à spec ("área de Equipe é a mesma tela com visões diferentes").

**Build gate**: `dotnet build Societiza.sln` — zero errors antes de avançar.

---

### Fase 3 — Application

**Referência**: `.claude/skills/architecture/references/application-architecture.md`

#### 1. Validator `UpdateUserNameValidator`
- **Camada**: Application
- **Tipo**: Validator
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Validators/User/UpdateUserNameValidator.cs`
- **Depende de**: Fase 2 · #1
- **Spec-ref**: logic §3 (sem regras estruturais novas — só formato)
- **Detalhes**: `RuleFor(FirstName).NotEmpty().MaximumLength(80)`; `RuleFor(LastName).NotEmpty().MaximumLength(80)`. Mensagens em inglês.

#### 2. Validator `PromoteUserToAccountancyAdminValidator`
- **Camada**: Application
- **Tipo**: Validator
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Validators/User/PromoteUserToAccountancyAdminValidator.cs`
- **Depende de**: Fase 2 · #2
- **Spec-ref**: logic §2 (transição de perfil — guarda de input)
- **Detalhes**: `RuleFor(AccountancyId).NotEmpty()`; `RuleFor(UserId).NotEmpty()`. Mensagens em inglês.

#### 3. Validator `DeleteUserValidator`
- **Camada**: Application
- **Tipo**: Validator
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Validators/User/DeleteUserValidator.cs`
- **Depende de**: Fase 2 · #3
- **Spec-ref**: logic §3 (regras de deletion — guarda de input)
- **Detalhes**: `RuleFor(UserId).NotEmpty()`. `AccountancyId` é nullable por design (SystemAdmin passa, AccountancyAdmin tem ignorado).

#### 4. Validator `ListAccountancyMembersValidator`
- **Camada**: Application
- **Tipo**: Validator
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Validators/User/ListAccountancyMembersValidator.cs`
- **Depende de**: Fase 2 · #9
- **Spec-ref**: logic §3 "AccountancyAdmin age apenas dentro da própria contabilidade"
- **Detalhes**: `RuleFor(AccountancyId).NotEmpty()`. Cross-tenant é validado no Handler.

#### 5. Handler `UpdateUserNameHandler`
- **Camada**: Application
- **Tipo**: Handler (Command)
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Commands/User/UpdateUserNameHandler.cs`
- **Depende de**: Fase 1 · #4, Fase 2 · #1
- **Spec-ref**: logic §3 "Todo usuário pode editar apenas o próprio nome"
- **Detalhes**: Injeta `IIdentityApplicationService` e `ICurrentUserService`.
  1. `userId = currentUser.UserId`.
  2. `LoadAggregateAsync<User>(userId)` — `NotFoundError` se não existir. **Nota**: `LoadAggregateAsync` já falha com `ConflictError` se `IsDeleted == true`, então não há necessidade de checagem extra de estado deletado.
  3. `user.UpdateName(request.FirstName, request.LastName)`.
  4. `AppendEventsAsync(user)`.
  5. Retorna `Result.Success()`.
  - Não há checagem cross-user porque o command não expõe `UserId` alvo — autorização é estrutural (só pode editar quem está logado).

#### 6. Handler `PromoteUserToAccountancyAdminHandler`
- **Camada**: Application
- **Tipo**: Handler (Command)
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Commands/User/PromoteUserToAccountancyAdminHandler.cs`
- **Depende de**: Fase 1 · #4, Fase 2 · #2, Fase 2 · #5
- **Spec-ref**: spec §8 "Promoção por SystemAdmin: Não"; logic §1 fluxo AA promover; logic §3 regras de promoção; logic §4 "resposta inclui o novo perfil"
- **Detalhes**: Injeta `IIdentityApplicationService`, `ICurrentUserService`, `IIdentityProjection<ProjectionModel.User>`.
  1. `callerRole = UserRole.FromName(currentUser.Role)` — se `SystemAdmin`, retornar `Result.Failure(new ForbiddenError(new("User.ForbiddenForSystemAdmin", "System administrators cannot promote members.")))`.
  2. Se `callerRole != AccountancyAdmin` → `ForbiddenError` ("Only an accountancy administrator can promote members.").
  3. Validar tenant: `currentUser.AccountancyId == request.AccountancyId` (path) — caso contrário `ForbiddenError`.
  4. `LoadAggregateAsync<User>(request.UserId)` — `NotFoundError` se não achar. **Nota**: `LoadAggregateAsync` já falha com `ConflictError` se `IsDeleted == true`, então usuários deletados não podem ser promovidos.
  5. Verificações no aggregate: `user.AccountancyId == request.AccountancyId` (mesmo tenant); `user.Role == UserRole.AccountancyEmployee` (cannot promote AA to AA).
  6. `user.PromoteToAccountancyAdmin(currentUser.UserId)`.
  7. `AppendEventsAsync(user)`.
  8. Retornar `UserDetailResponse` montado a partir do aggregate (Id, First/Last, Email, Role atualizado, CreatedAt, InvitationLinkId via projection lookup ou `null` — ver detalhe abaixo).
  - Para `InvitationLinkId` na resposta: ler da projection `userProjection.FindAsync(x => x.Id == user.Id)` antes de retornar — campo já existe na projection.

#### 7. Handler `DeleteUserCommandHandler`
- **Camada**: Application
- **Tipo**: Handler (Command)
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Commands/User/DeleteUserCommandHandler.cs`
- **Depende de**: Fase 1 · #4, Fase 2 · #3
- **Spec-ref**: spec §8 (linhas: exclusão por SA, exclusão por AA, último admin, autoexclusão, soft delete, sessão); logic §1 fluxos de remoção; logic §3 todas as regras de remoção; logic §5 todos os edge cases de remoção
- **Detalhes**: Injeta `IIdentityApplicationService`, `ICurrentUserService`, `IIdentityProjection<ProjectionModel.User>`.
  1. **Resolver tenant**: `callerRole = UserRole.FromName(currentUser.Role)`.
     - Se `SystemAdmin`: `effectiveAccountancyId = request.AccountancyId` (do query param).
     - Se `AccountancyAdmin`: `effectiveAccountancyId = currentUser.AccountancyId` (claim) — query param ignorado.
     - Se `AccountancyEmployee` ou outra → `ForbiddenError` ("You do not have permission to remove users.").
  2. **Carregar alvo**: `targetResult = LoadAggregateAsync<User>(request.UserId)` → `NotFoundError`. **Nota**: `LoadAggregateAsync` já falha com `ConflictError` se `IsDeleted == true`, garantindo idempotência (segunda chamada de delete sobre o mesmo usuário retorna conflict).
  3. **Bloquear autoexclusão**: `if (targetUser.Id == currentUser.UserId)` → `Result.Failure(new ConflictError(new("User.CannotRemoveSelf", "Cannot remove yourself.")))`.
  4. **Cross-tenant guard (AA)**: se caller é AA e `targetUser.AccountancyId != currentUser.AccountancyId` → `ForbiddenError`.
  5. **Bloqueio "último AccountancyAdmin"**: se `targetUser.Role == AccountancyAdmin` e `targetUser.AccountancyId is not null`:
     - `count = userProjection.CountAsync(x => x.AccountancyId == targetUser.AccountancyId && x.Role == UserRole.AccountancyAdmin.Name && !x.IsDeleted)`. Se ≤ 1 → `ConflictError("User.LastAccountancyAdmin", "Cannot remove the last administrator of the accountancy.")`.
     - **Atomicidade**: a checagem é leitura na projection seguida de escrita no event store. O write é serializado pelo `UnitOfWork` + concorrência otimista do event store (chave `(AggregateId, Version)`). Para garantir corrida, usar `IUnitOfWork<IdentityDbContext>` com transação serializável **OU** depender do replay determinístico das projections + retry. **Decisão deste plano**: usar a estratégia atual do projeto (UnitOfWork + version concurrency); o edge case "dois AAs simultâneos" é mitigado porque o segundo write falha por estado: ao recarregar o aggregate, vê que já só sobra um e bloqueia. Ver "Riscos e pontos de atenção".
  6. **Bloqueio "último SystemAdmin"**: se `targetUser.Role == SystemAdmin`:
     - `count = userProjection.CountAsync(x => x.Role == UserRole.SystemAdmin.Name && !x.IsDeleted)`. Se ≤ 1 → `ConflictError("User.LastSystemAdmin", "Cannot remove the last system administrator.")`.
  7. `targetUser.Delete(currentUser.UserId)` — método já chama internamente `RevokeAllRefreshTokens()` e `RegenerateSecurityStamp()` (ver Fase 1 #4).
  8. `AppendEventsAsync(targetUser)` — persiste eventos `UserDeleted`, `UserRefreshTokenRevoked` (vários) e `UserSecurityStampRegenerated` na mesma transação. Após commit, todos publicados em ordem.
  9. Retornar `Result.Success()`.

#### 8. Handler `GetMyProfileHandler`
- **Camada**: Application
- **Tipo**: Handler (Query)
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Queries/User/GetMyProfileHandler.cs`
- **Depende de**: Fase 2 · #4, Fase 2 · #7
- **Spec-ref**: spec §1 "perfil do próprio usuário"
- **Detalhes**: Injeta `IIdentityProjection<ProjectionModel.User>` e `ICurrentUserService`.
  1. `userView = userProjection.FindAsync(x => x.Id == currentUser.UserId && !x.IsDeleted)`.
  2. Se `null` → `NotFoundError("User.NotFound", "User not found.")`.
  3. Mapear para `UserProfileResponse` e `Result.Success`.

#### 9. Handler `ListSystemAdminsHandler`
- **Camada**: Application
- **Tipo**: Handler (Query)
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Queries/User/ListSystemAdminsHandler.cs`
- **Depende de**: Fase 2 · #5, Fase 2 · #8
- **Spec-ref**: spec §1 "SystemAdmin enxerga a lista de outros SystemAdmins"; logic §1 SystemAdmin (painel administrativo)
- **Detalhes**: Injeta `IIdentityProjection<ProjectionModel.User>`. Sem `ICurrentUserService` aqui — autorização ocorre no endpoint via `[Authorize(Policy = CanManageAccountancies)]` (única role que cobre SystemAdmin sozinho hoje). Listar `x => x.Role == UserRole.SystemAdmin.Name && !x.IsDeleted`. Mapear para `UserDetailResponse[]` ordenado por `CreatedAt` descendente.

#### 10. Handler `ListAccountancyMembersHandler`
- **Camada**: Application
- **Tipo**: Handler (Query)
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Queries/User/ListAccountancyMembersHandler.cs`
- **Depende de**: Fase 2 · #5, Fase 2 · #6, Fase 2 · #9, Fase 2 · #10
- **Spec-ref**: spec §1 "área de Equipe"; spec §8 "Dados visíveis na listagem"; logic §3 (filtragem por perfil) e §5 (último edge case — backend filtra dados independentemente do que o frontend pede)
- **Detalhes**: Injeta `IIdentityProjection<ProjectionModel.User>` e `ICurrentUserService`.
  1. `callerRole = UserRole.FromName(currentUser.Role)`.
  2. **Cross-tenant guard**: se `callerRole != SystemAdmin` e `currentUser.AccountancyId != request.AccountancyId` → `ForbiddenError("User.ForbiddenTenant", "You can only list members of your own accountancy.")`.
  3. Listar `x => x.AccountancyId == request.AccountancyId && !x.IsDeleted` ordenado por `Role` (admins primeiro) e depois por `CreatedAt`.
  4. **Filtro por perfil**:
     - Se caller é `AccountancyAdmin` ou `SystemAdmin`: mapear para `UserDetailResponse[]` → retornar `AccountancyMembersResponse(Members: [...], Summaries: null)`.
     - Se caller é `AccountancyEmployee`: mapear para `UserSummaryResponse[]` (sem email, sem JoinedAt, sem InvitationLinkId) → retornar `AccountancyMembersResponse(Members: null, Summaries: [...])`.

#### 11. Projection handler `ProjectionUserEventHandler` — eventos novos
- **Camada**: Application
- **Tipo**: ProjectionHandler (modificação)
- **Ação**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Events/ProjectionUserEventHandler.cs`
- **Depende de**: Fase 1 · #1, #2, #3, #5
- **Spec-ref**: logic §3 "Listagem de membros ativos"; logic §4 BE precisa garantir
- **Detalhes**:
  - Estender `IProjectionUserEventHandler` com `IEventHandler<DomainEvents.UserNameUpdated>`, `IEventHandler<DomainEvents.UserPromotedToAccountancyAdmin>`, `IEventHandler<DomainEvents.UserDeleted>`.
  - 3 novos métodos `Handle`:
    - `UserNameUpdated`: `FindAsync(x => x.Id == @event.UserId)` → `existing with { FirstName = ..., LastName = ..., UpdatedAt = ... }` → `ReplaceInsertAsync`. Idempotente: se `existing is null`, log warning e return.
    - `UserPromotedToAccountancyAdmin`: idem com `Role = @event.Role` e `UpdatedAt = @event.PromotedAt`.
    - `UserDeleted`: idem com `IsDeleted = true` e `UpdatedAt = @event.DeletedAt`.
  - Cada handler envolto em `try/catch` com rethrow `InvalidOperationException` para retry MassTransit (padrão do projeto).
  - **Não modificar** `UserRegistrationCompleted` handler — projection já é criada com `IsDeleted = false` por convenção da base.

#### 12. (Opcional) Não-item — `RefreshTokenHandler` e `Auth/RefreshHandler` checam `IsDeleted`?
- **Decisão**: **fora do escopo**. A invalidação de sessão acontece via `RevokeAllRefreshTokens` + `RegenerateSecurityStamp` (que altera o `SecurityStamp` no aggregate). O `RefreshHandler` existente já valida o token contra o aggregate carregado — ao tentar refresh, verá que o token foi revogado e o `SecurityStamp` mudou. Próxima request com access token antigo passará pelo middleware de validação do JWT que (se configurado para checar o stamp) rejeita. **Verificar**: durante a implementação, confirmar se o middleware atual já compara `SecurityStamp` do JWT com o do aggregate. Se não, virar item explícito no plano. Ver "Riscos e pontos de atenção".

**Build gate**: `dotnet build Societiza.sln` — zero errors antes de avançar.

---

### Fase 4 — Persistence

**Referência**: `.claude/skills/architecture/references/persistence-architecture.md`

#### 1. Migration — sem novas tabelas
- **Camada**: Persistence
- **Tipo**: Migration
- **Ação**: avaliar (provavelmente "nenhuma")
- **Caminho**: `src/Modules/Identity/Identity.Persistence/Migrations/`
- **Depende de**: Fase 1 · #1, #2, #3, #4
- **Spec-ref**: arquitetura — eventos novos e propriedades novas no aggregate `User`
- **Detalhes**: O aggregate `User` é persistido como JSON em `UserStoreEvents` (já existente) e `UserSnapshots` (já existente). Eventos novos (`UserNameUpdated`, `UserPromotedToAccountancyAdmin`, `UserDeleted`) são gravados na mesma tabela `UserStoreEvents` — não exigem schema novo. Não há propriedades novas no aggregate; o soft-delete usa a flag `IsDeleted` herdada da base `Entity`, já presente no snapshot.
  - **Comando esperado**: rodar `dotnet ef migrations add UserManagementNoOp --project src/Modules/Identity/Identity.Persistence --startup-project src/Web --context IdentityDbContext` apenas para confirmar que **não há diff** de schema. Se EF gerar uma migration vazia, descartar (`dotnet ef migrations remove`).
  - **Se houver diff inesperado**: investigar antes de aplicar.

**Build gate**: `dotnet build Societiza.sln` — zero errors antes de avançar.

---

### Fase 5 — Infrastructure

**Referência**: `.claude/skills/architecture/references/infrastructure-architecture.md`

Não há novos serviços nem registros de open generics. Os handlers e validators
são auto-discovered via MediatR/FluentValidation scan que já roda em
`ApplicationServiceInstaller`. Cross-module: nenhum — esta feature é
inteiramente Identity.

**Itens**: nenhum.

**Build gate**: `dotnet build Societiza.sln` — zero errors antes de avançar.

---

### Fase 6 — Web

**Referência**: `.claude/skills/architecture/references/web-architecture.md` + skill `/api-design`

#### 1. Rotas novas em `IdentityRoutes`
- **Camada**: Web
- **Tipo**: Routes (modificação)
- **Ação**: modificar
- **Caminho**: `src/Web/Endpoints/Routes/IdentityRoutes.cs`
- **Depende de**: nenhum
- **Spec-ref**: contrato de endpoints (sessão)
- **Detalhes**: Adicionar constantes:
  - `internal const string UserId = "userId";`
  - `internal const string AccountancyIdParam = "accountancyId";`
  - `internal const string GetMyProfile           = $"{BaseUri}/users";`
  - `internal const string UpdateMyName           = $"{BaseUri}/users";`
  - `internal const string ListSystemAdmins       = $"{BaseUri}/system-admins";`
  - `internal const string DeleteUser             = $"{BaseUri}/users/{{{UserId}:guid}}";`
  - `internal const string ListAccountancyMembers = $"{BaseUri}/accountancies/{{{AccountancyIdParam}:guid}}/members";`
  - `internal const string PromoteAccountancyMember = $"{BaseUri}/accountancies/{{{AccountancyIdParam}:guid}}/members/{{{UserId}:guid}}/promote";`

#### 2. Request DTO `UpdateMyNameRequest`
- **Camada**: Web
- **Tipo**: RequestDTO
- **Ação**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/User/Requests/Commands/UpdateMyNameRequest.cs`
- **Depende de**: nenhum
- **Spec-ref**: spec §8 "Edição do próprio perfil"
- **Detalhes**: `sealed class` com `string FirstName { get; set; } = string.Empty; string LastName { get; set; } = string.Empty;` (FromBody implícito).

#### 3. Request DTO `DeleteUserRequest`
- **Camada**: Web
- **Tipo**: RequestDTO
- **Ação**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/User/Requests/Commands/DeleteUserRequest.cs`
- **Depende de**: Fase 6 · #1
- **Spec-ref**: contrato `DELETE /users/{userId}?accountancyId=...`
- **Detalhes**: `[FromRoute(Name = IdentityRoutes.UserId)] Guid UserId`; `[FromQuery] Guid? AccountancyId`.

#### 4. Request DTO `PromoteAccountancyMemberRequest`
- **Camada**: Web
- **Tipo**: RequestDTO
- **Ação**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/User/Requests/Commands/PromoteAccountancyMemberRequest.cs`
- **Depende de**: Fase 6 · #1
- **Spec-ref**: contrato `POST /accountancies/{accountancyId}/members/{userId}/promote`
- **Detalhes**: `[FromRoute(Name = IdentityRoutes.AccountancyIdParam)] Guid AccountancyId`; `[FromRoute(Name = IdentityRoutes.UserId)] Guid UserId`.

#### 5. Request DTO `ListAccountancyMembersRequest`
- **Camada**: Web
- **Tipo**: RequestDTO
- **Ação**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/User/Requests/Queries/ListAccountancyMembersRequest.cs`
- **Depende de**: Fase 6 · #1
- **Spec-ref**: contrato `GET /accountancies/{accountancyId}/members`
- **Detalhes**: `[FromRoute(Name = IdentityRoutes.AccountancyIdParam)] Guid AccountancyId`.

#### 6. Endpoint `GetMyProfileEndpoint`
- **Camada**: Web
- **Tipo**: Endpoint
- **Ação**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/User/GetMyProfileEndpoint.cs`
- **Depende de**: Fase 2 · #4, #7; Fase 3 · #8; Fase 6 · #1
- **Spec-ref**: spec §1 "perfil do próprio usuário"
- **Detalhes**:
  - `[Authorize]` (apenas autenticação — qualquer role).
  - `[HttpGet(IdentityRoutes.GetMyProfile)]`, `[ApiVersion("1.0")]`.
  - `[ProducesResponseType(typeof(UserProfileResponse), 200)]`, 400, 401, 403, 404.
  - `OperationId = "GetMyProfile"`, `Summary = "Obter perfil do próprio usuário"`, `Tags = [Tags.Identity.Users]`.
  - Pipeline: `Result.Create(request).Map(_ => new GetMyProfileQuery()).Bind(...).Match(Ok, this.HandleFailure)`.
  - Request DTO vazio (sem fields) — usar `EmptyRequest` ou criar `GetMyProfileRequest` vazio para compatibilidade com Ardalis.

#### 7. Endpoint `UpdateMyNameEndpoint`
- **Camada**: Web
- **Tipo**: Endpoint
- **Ação**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/User/UpdateMyNameEndpoint.cs`
- **Depende de**: Fase 2 · #1; Fase 3 · #5; Fase 6 · #1, #2
- **Spec-ref**: spec §8 "Edição do próprio perfil"
- **Detalhes**:
  - `[Authorize]`.
  - `[HttpPut(IdentityRoutes.UpdateMyName)]`, `[ApiVersion("1.0")]`.
  - `[ProducesResponseType(200)]`, 400, 401, 403, 404.
  - `OperationId = "UpdateMyName"`, `Summary = "Atualizar nome do próprio usuário"`, `Tags = [Tags.Identity.Users]`.
  - Pipeline: `Result.Create(request).Map(r => new UpdateUserNameCommand(r.FirstName, r.LastName)).Bind(...).Match(() => Ok(), this.HandleFailure)`.

#### 8. Endpoint `ListSystemAdminsEndpoint`
- **Camada**: Web
- **Tipo**: Endpoint
- **Ação**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/User/ListSystemAdminsEndpoint.cs`
- **Depende de**: Fase 2 · #5, #8; Fase 3 · #9; Fase 6 · #1
- **Spec-ref**: spec §1 SystemAdmin painel administrativo
- **Detalhes**:
  - `[Authorize(Policy = IdentityPolicies.CanManageAccountancies)]` — única policy hoje que isola `SystemAdmin`. Reaproveitada (sem criar policy nova).
  - `[HttpGet(IdentityRoutes.ListSystemAdmins)]`, `[ApiVersion("1.0")]`.
  - `[ProducesResponseType(typeof(IReadOnlyList<UserDetailResponse>), 200)]`, 400, 401, 403.
  - `OperationId = "ListSystemAdmins"`, `Summary = "Listar SystemAdmins ativos"`, `Tags = [Tags.Identity.Users]`.
  - Request DTO vazio (`ListSystemAdminsRequest` sem campos).

#### 9. Endpoint `ListAccountancyMembersEndpoint`
- **Camada**: Web
- **Tipo**: Endpoint
- **Ação**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/User/ListAccountancyMembersEndpoint.cs`
- **Depende de**: Fase 2 · #9, #10; Fase 3 · #10; Fase 6 · #1, #5
- **Spec-ref**: spec §1 "área de Equipe"
- **Detalhes**:
  - `[Authorize]` (qualquer role autenticada — handler decide o filtro de payload e o cross-tenant).
  - `[HttpGet(IdentityRoutes.ListAccountancyMembers)]`, `[ApiVersion("1.0")]`.
  - `[ProducesResponseType(typeof(AccountancyMembersResponse), 200)]`, 400, 401, 403, 404.
  - `OperationId = "ListAccountancyMembers"`, `Summary = "Listar membros da contabilidade"`, `Tags = [Tags.Identity.Users]`.
  - Pipeline: `Map(r => new ListAccountancyMembersQuery(r.AccountancyId))`.

#### 10. Endpoint `PromoteAccountancyMemberEndpoint`
- **Camada**: Web
- **Tipo**: Endpoint
- **Ação**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/User/PromoteAccountancyMemberEndpoint.cs`
- **Depende de**: Fase 2 · #2, #5; Fase 3 · #6; Fase 6 · #1, #4
- **Spec-ref**: spec §8 "Promoção AE → AA"; logic §1 fluxo AA promover
- **Detalhes**:
  - `[Authorize(Policy = IdentityPolicies.CanPromoteUser)]` — policy já existente, restrita a `AccountancyAdmin`. Bloqueio de SystemAdmin é estrutural (a policy não inclui SA), e o handler também rejeita defensivamente.
  - `[HttpPost(IdentityRoutes.PromoteAccountancyMember)]`, `[ApiVersion("1.0")]`.
  - `[ProducesResponseType(typeof(UserDetailResponse), 200)]`, 400, 401, 403, 404, 409.
  - `OperationId = "PromoteAccountancyMember"`, `Summary = "Promover membro a administrador"`, `Tags = [Tags.Identity.Users]`.
  - Pipeline: `Map(r => new PromoteUserToAccountancyAdminCommand(r.AccountancyId, r.UserId))`.

#### 11. Endpoint `DeleteUserEndpoint`
- **Camada**: Web
- **Tipo**: Endpoint
- **Ação**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/User/DeleteUserEndpoint.cs`
- **Depende de**: Fase 2 · #3; Fase 3 · #7; Fase 6 · #1, #3
- **Spec-ref**: spec §8 "Soft delete unificado"; logic §1 fluxos de remoção; logic §3 regras
- **Detalhes**:
  - `[Authorize(Policy = IdentityPolicies.CanManageUsers)]` — policy já existente cobre `SystemAdmin` e `AccountancyAdmin`. AE bloqueado.
  - `[HttpDelete(IdentityRoutes.DeleteUser)]`, `[ApiVersion("1.0")]`.
  - `[ProducesResponseType(200)]`, 400, 401, 403, 404, 409.
  - `OperationId = "DeleteUser"`, `Summary = "Deletar usuário"`, `Tags = [Tags.Identity.Users]`.
  - Pipeline: `Map(r => new DeleteUserCommand(r.UserId, r.AccountancyId))`.

#### 12. MassTransit consumer `ProjectionUserConsumer` — 3 eventos novos
- **Camada**: Web
- **Tipo**: ConsumerRegistration (modificação)
- **Ação**: modificar
- **Caminho**: `src/Web/ServiceInstallers/EventBus/Consumers/IdentityModule/ProjectionUserConsumer.cs` e `ConsumerConfiguration.cs`
- **Depende de**: Fase 1 · #1, #2, #3; Fase 3 · #11
- **Spec-ref**: arquitetura — todo evento novo precisa de queue
- **Detalhes**:
  1. Em `ProjectionUserConsumer.cs`: adicionar `IConsumer<DomainEvents.UserNameUpdated>`, `IConsumer<DomainEvents.UserPromotedToAccountancyAdmin>`, `IConsumer<DomainEvents.UserDeleted>` à lista de interfaces. Adicionar 3 métodos `Consume(...)` que delegam ao `eventHandler.Handle(context.Message, context.CancellationToken)`.
  2. Em `ConsumerConfiguration.cs`: 3 novas chamadas a `bus.ConfigureEventReceiveEndpoint<ProjectionUserConsumer, DomainEvents.{Event}>(context, MODULE_NAME)`.

**Build gate**: `dotnet build Societiza.sln` — zero errors. Depois: `dotnet test Societiza.sln` — todos passando (especial atenção ao `EndpointConventionsTests` em `tests/Web.Tests/`).

---

## Checklist de registro

- [x] **`ConsumerConfiguration.cs`** — `ConfigureEventReceiveEndpoint` por evento novo
  - **Arquivo**: `src/Web/ServiceInstallers/EventBus/Consumers/IdentityModule/ConsumerConfiguration.cs`
  - **O que adicionar**: 3 chamadas a `ConfigureEventReceiveEndpoint<ProjectionUserConsumer, T>` para `UserNameUpdated`, `UserPromotedToAccountancyAdmin`, `UserDeleted`.

- [ ] **`AddEventInteractors()`** — registro de projection handlers novos
  - **Status**: **inalterado**. Os 3 eventos novos são processados pelo `ProjectionUserEventHandler` existente (apenas adicionando métodos `Handle`). O handler já está registrado.

- [ ] **`MigrateDatabaseStartupTask`** — registro de DbContext novo
  - **Status**: **inalterado**. Sem `DbContext` novo.

- [ ] **`RegisterRecurringJobsStartupTask`** — registro de jobs recorrentes
  - **Status**: **inalterado**. Sem job recorrente nesta feature.

- [ ] **`PersistenceServiceInstaller`** — registro explícito de open generics
  - **Status**: **inalterado**. Sem novos open generics.

- [ ] **`Web.csproj`** — referência aos projetos novos
  - **Status**: **inalterado**. Sem módulo novo.

- [ ] **`CrossModuleServicesInstaller`** — serviços cross-module
  - **Status**: **inalterado**. Esta feature é interna ao Identity.

---

## Riscos e pontos de atenção

### 1. Divergência intencional com a spec/logic — sem endpoints de listagem de deletados

A spec §1 e logic §1 mencionam "visão de inativos" (separada, para admins). Decisão
da sessão registrada no briefing: **não há endpoints de listagem de usuários deletados**.
O plano honra a decisão da sessão; nenhuma rota nem query lista usuários deletados.
A interface admin simplesmente não terá tab de deletados no MVP. A presença da flag
`IsDeleted` na projection (herdada da base) mantém a porta aberta para adicionar a
feature futuramente sem migração.

### 2. Atomicidade do "último administrador" sob concorrência

Logic §5 edge case "Dois AccountancyAdmins simultâneos" exige verificação atômica.
A estratégia deste plano:

- Leitura via projection (MongoDB) para contar admins ativos antes de iniciar a
  deleção.
- Escrita via event store com concorrência otimista por `(AggregateId, Version)`.
- O segundo solicitante a tentar deletar carrega o aggregate com `Version` atual,
  mas a checagem de "último admin" é baseada na projection — que pode estar
  desatualizada (eventual consistency).

**Cenário problemático**: ambos passam pela checagem de projection (≥ 2 admins
no momento da leitura), ambos chegam ao `AppendEventsAsync`, ambos têm sucesso.
Resultado: contabilidade fica órfã.

**Mitigação proposta** (a discutir com o humano antes de implementar):
- **Opção A** (pragmática, mvp): Aceitar a janela de race ínfima — em produção
  real a probabilidade é baixíssima (dois admins clicando simultaneamente em
  uma contabilidade de exatamente 2 admins). Adicionar log de auditoria
  pós-fato; quando detectada uma contabilidade órfã, rerodar aggregate replay
  para verificação. **Padrão atual do projeto** segue Opção A para
  `InvitationLink` (regra "um link ativo por escopo" é checagem soft).
- **Opção B** (robusta): Adicionar lock pessimista no `IdentityDbContext` via
  `SELECT ... FOR UPDATE` na linha do "marcador" da contabilidade. Custo:
  introduz coupling a uma tabela de marcador que hoje não existe. Provavelmente
  fora do escopo do MVP.
- **Opção C**: Mover a contagem para uma query SQL dedicada que use `SERIALIZABLE`
  + retry. Custo: quebra a regra "nunca ler do PostgreSQL no read side".

**Recomendação deste plano**: Opção A — alinhada ao padrão do projeto.
Documentar o cenário no comentário do handler. Pedir validação explícita do humano.

### 3. Verificação do `SecurityStamp` no middleware de JWT

A invalidação de sessão pós-deleção depende de:
1. `RevokeAllRefreshTokens` — invalida refresh tokens (cobre `POST /auth/refresh`).
2. `RegenerateSecurityStamp` — invalida access tokens **se** o middleware de JWT
   comparar o `stamp` do token contra o `SecurityStamp` atual do aggregate em
   cada request autenticada.

**A confirmar durante implementação**: Ler o middleware atual de validação JWT
(`src/Web/...` — provavelmente em `EventBusServiceInstaller` ou `Middlewares`).
Se a comparação de stamp não existir, a invalidação de access token não acontece
até o token expirar naturalmente. Logic §3 exige "imediata" — pode ser necessário
adicionar checagem `OnTokenValidated` no `JwtBearerEvents`. Esse trabalho, se
necessário, vira **item adicional** na Fase 6 e precisa de aprovação. Por ora,
a hipótese deste plano é que a verificação já existe (consistente com fluxo de
logout que já chama `RegenerateSecurityStamp`).

### 4. Resposta heterogênea em `AccountancyMembersResponse`

A escolha de retornar um único response com `Members?` e `Summaries?` (apenas
um deles preenchido) gera DTO menos limpo, mas evita criar dois endpoints
distintos para a mesma "tela de Equipe". Alternativa: criar dois endpoints
(`/members` para admins, `/members/summary` para AE) — mais REST, mas duplica
contrato. Pedir validação explícita do humano antes de cristalizar.

### 5. `InvitationLinkId` na projection é nullable e pode estar ausente em registros antigos

A projection `User` já carrega `InvitationLinkId` (foi adicionada em feature
anterior). Para registros muito antigos pode estar `null`. Endpoints de listagem
admin retornam o campo como opcional — frontend deve aceitar `null`.

### 6. Caminho do migration "no-op"

A Fase 4 sugere rodar `dotnet ef migrations add` apenas para confirmar que não
há diff. Se a migration vier vazia, **descartar** com `dotnet ef migrations remove`.
Se houver diff inesperado (ex: EF detectar mudança em coluna por causa de
metadata sutil), parar e investigar antes de aplicar.

### 7. Mensagens de erro padronizadas

Reutilizar exatamente os textos definidos na sessão:
- "Cannot remove the last administrator of the accountancy."
- "Cannot remove the last system administrator."
- "Cannot remove yourself."
- "Only the user can edit their own name." (não usado diretamente — autorização
  é estrutural via `ICurrentUserService.UserId`; mensagem reservada caso
  futura variante de endpoint receba `userId` no payload).
- "System administrators cannot promote members."

Todas em inglês conforme regra de memória do usuário.

### 8. Tag `Identity.Users` já existe em `Tags.cs`

Confirmado em `src/Web/Endpoints/Tags.cs`: `internal const string Users = "Identity / 03. Users";`. Sem alteração no arquivo `Tags.cs`.