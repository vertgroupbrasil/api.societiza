# Plano — identity-invite-link

**Spec**: specs/identity-invite-link/spec.md @ product.societiza
**Modulos afetados**: Identity (refactor + extensao), Accountancy (leitura cross-module), Web
**Modulo novo?**: nao
**Resumo**: Adiciona um link de equipe por contabilidade (7 dias de validade, role fixo `AccountingEmployee`) que o `AccountingAdmin` compartilha por WhatsApp. Refatora o aggregate `User` para suportar **multi-membership** (mesmo usuario em varias contabilidades — caso de contador autonomo). Adiciona remocao de membros pelo `AccountingAdmin`. Os dois fluxos do `SystemAdmin` (convite de `SystemAdmin` e de `AccountingAdmin` de nova contabilidade) continuam por email, mas passam a retornar o link na resposta. Nao inclui notificacoes in-app (explicitamente fora do escopo pela spec).

### Como o link se conecta entre os modulos

- **Identity e dono** do aggregate `AccountancyInviteLink` — ele conhece usuarios, roles e tokens. A "contabilidade de destino" entra como `AccountancyId` no stream — Identity nao importa a estrutura interna do Accountancy, apenas o identificador.
- **Accountancy nao conhece o link** — zero referencia reversa. Isso mantem a fronteira limpa e evita dependencia circular.
- **Handler publico de info do link** (pagina de registro anonima) le o **nome da contabilidade** via `IAccountancyProjection<ProjectionModel.Accountancy>` — unica passagem cross-module, somente leitura.
- **Id deterministico** (GUIDv5 sobre `AccountancyId`) garante que existe exatamente um stream de link por contabilidade sem lookup previo — alinhado com a regra "exatamente um link de equipe ativo por contabilidade" (spec §7).

---

## Pre-condicoes

1. **Remocao de membros entra junto** — spec §6 declara "as duas features andam juntas". O plano inclui o endpoint `DELETE /accountancies/{id}/members/{userId}` e a mecanica de saida no aggregate `User`.
2. **Multi-membership no aggregate `User`** — spec §6, §9 afirmam que o mesmo email pode pertencer a varias contabilidades. O plano refatora `User` para expor uma lista `Memberships` no lugar do `AccountancyId`/`Role` escalares. Projection `User` passa a expor `Memberships[]`. Existing event streams sao compativeis via replay (eventos antigos continuam raiseando `UserRegistrationCompleted` — novo handler traduz para um `UserMembershipAdded` sintetico ao hidratar). **Este e o ponto de maior risco do plano — ver §Riscos**.
3. **Sem orquestrador** para o cenario SysAdmin → AccountingAdmin — o frontend chama sequencialmente `POST /accountancies` e `POST /identity/invite`. Spec §9 confirma.
4. **Sem notificacoes** — modulo `Notifications` sai do escopo (spec §7). Evento `InviteLinkUsed` e publicado mas nao ha consumidor nesta spec.
5. **Expiracao de link e manual + job recorrente** — job recorrente horario marca links com `ExpiresAt < now` como `Expired`. `AccountingAdmin` regenera manualmente quando precisa. Sem aviso previo (spec §7).
6. **`FrontendOptions.BaseUrl` ja existe** — reutilizado para montar URLs de join e de accept-invite. Nao cria novo options binding.

---

## Itens

### Grupo A — Refactor multi-membership no aggregate `User`

### 1. Value object `UserMembership`

- **Camada**: Domain
- **Tipo**: ValueObject
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Domain/ValueObjects/UserMembership.cs`
- **Depende de**: nenhum
- **Spec-ref**: §6 "suporte a usuarios em multiplas contabilidades"
- **Detalhes**: `sealed record UserMembership(Guid AccountancyId, UserRole Role, DateTimeOffset JoinedAt)`. Equidade por `(UserId, AccountancyId)` — um usuario nunca tem duas memberships na mesma contabilidade (mesmo que o role mude, e substituido).

### 2. Domain events de membership

- **Camada**: Domain
- **Tipo**: DomainEvent
- **Acao**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Domain/Events/DomainEvents.cs`
- **Depende de**: #1
- **Spec-ref**: §6, §9
- **Detalhes**: Adicionar dois records:
  - `UserMembershipAdded(Guid UserId, Guid AccountancyId, string Role, DateTimeOffset JoinedAt, ulong Version)` — emitido no `Create` (membership inicial) e no `JoinAccountancy`. `Role` serializado como string via `.Name`.
  - `UserMembershipRemoved(Guid UserId, Guid AccountancyId, DateTimeOffset RemovedAt, ulong Version)` — emitido em `LeaveAccountancy`.
  - `UserRegistrationCompleted` mantido como esta (compat com streams existentes) — `When(UserRegistrationCompleted)` agora hidrata `Memberships` adicionando um item a partir do `AccountancyId`/`Role` do evento (equivalente a um `UserMembershipAdded` sintetico).

### 3. Refactor aggregate `User` — metodos e estado

- **Camada**: Domain
- **Tipo**: Aggregate
- **Acao**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Domain/Aggregates/User.cs`
- **Depende de**: #1, #2
- **Spec-ref**: §6, §9, §4 cenario 3
- **Detalhes**:
  - Novo estado: `IReadOnlyList<UserMembership> Memberships` (privado como `List<>`). Campos escalares `AccountancyId` e `Role` viram computed properties (`PrimaryAccountancyId = Memberships.FirstOrDefault()?.AccountancyId`) — mantem leitura de legacy onde necessario.
  - `Create(inviteId, firstName, lastName, email, passwordHash, role, accountancyId)` — mantido, continua raiseando `UserRegistrationCompleted` (nao quebra stream existente). Internamente `When` popula `Memberships` com um item.
  - **Novo metodo** `JoinAccountancy(Guid accountancyId, UserRole role, DateTimeOffset joinedAt)` — nao levanta se ja existe membership para aquela contabilidade (idempotente). Senao, raise `UserMembershipAdded`.
  - **Novo metodo** `LeaveAccountancy(Guid accountancyId, DateTimeOffset removedAt)` — se nao tem membership, `NoOp` (idempotente). Senao, raise `UserMembershipRemoved`. Nao deleta o user (continua acessando outras contabilidades).
  - `When(UserMembershipAdded)` e `When(UserMembershipRemoved)` hidratam `Memberships`.

### 4. Projection model `User` — expor `Memberships[]`

- **Camada**: Domain
- **Tipo**: ProjectionModel
- **Acao**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Domain/Projection/ProjectionModel.cs`
- **Depende de**: #1
- **Spec-ref**: §6, §9
- **Detalhes**: Substituir `AccountancyId` (Guid?) e `Role` (string) por `Memberships: IReadOnlyList<MembershipEntry>` onde `MembershipEntry(Guid AccountancyId, string Role, DateTimeOffset JoinedAt)`. Campos escalares (`AccountancyId`, `Role`) podem permanecer como propriedades derivadas opcionais se outros modulos leem diretamente — **verificar uso antes de remover** (ver checklist).

### 5. Projection handler `User` — novos eventos + compat

- **Camada**: Application
- **Tipo**: ProjectionHandler
- **Acao**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Events/ProjectionUserEventHandler.cs`
- **Depende de**: #2, #4
- **Spec-ref**: §6, §9
- **Detalhes**:
  - Interface composta adiciona `IEventHandler<UserMembershipAdded>` e `IEventHandler<UserMembershipRemoved>`.
  - `Handle(UserRegistrationCompleted)` continua inserindo o documento; passa a popular `Memberships: [new(AccountancyId, Role, CreatedAt)]`.
  - `Handle(UserMembershipAdded)` — `existing with { Memberships = existing.Memberships.Append(new(...)).ToArray() }` (deduplicacao por `AccountancyId`).
  - `Handle(UserMembershipRemoved)` — `existing with { Memberships = existing.Memberships.Where(m => m.AccountancyId != ev.AccountancyId).ToArray() }`.

### 6. Rebuild da projection `User` apos deploy

- **Camada**: Persistence
- **Tipo**: Migration
- **Acao**: criar
- **Caminho**: N/A — procedimento manual documentado
- **Depende de**: #4, #5
- **Spec-ref**: §6 (multi-membership)
- **Detalhes**: Schema de `User` na Mongo muda. Estrategia MVP: script de rebuild que dropa a collection `User` e replaia todos os streams `UserStoreEvents` do Postgres via `IMediator`/replay service. Alternativa: migracao incremental lendo docs antigos e transformando para o novo shape. **Decisao pendente — ver §Riscos**.

### 7. Consumer atualizado — novos eventos de User

- **Camada**: Web
- **Tipo**: ConsumerRegistration
- **Acao**: modificar
- **Caminho**: `src/Web/ServiceInstallers/EventBus/Consumers/IdentityModule/ProjectionUserConsumer.cs`
- **Depende de**: #2
- **Spec-ref**: §6, §9
- **Detalhes**: Adicionar `IConsumer<DomainEvents.UserMembershipAdded>` e `IConsumer<DomainEvents.UserMembershipRemoved>` — cada `Consume` delega ao handler.

### 8. `ConsumerConfiguration` — dois novos endpoints

- **Camada**: Web
- **Tipo**: ConsumerRegistration
- **Acao**: modificar
- **Caminho**: `src/Web/ServiceInstallers/EventBus/Consumers/IdentityModule/ConsumerConfiguration.cs`
- **Depende de**: #7
- **Spec-ref**: obrigatorio por pattern
- **Detalhes**: `ConfigureEventReceiveEndpoint<ProjectionUserConsumer, UserMembershipAdded>` e idem para `UserMembershipRemoved`.

---

### Grupo B — Aggregate `AccountancyInviteLink`

### 9. SmartEnum `InviteLinkStatus`

- **Camada**: Domain
- **Tipo**: SmartEnum
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Domain/Enumerations/InviteLinkStatus.cs`
- **Depende de**: nenhum
- **Spec-ref**: §9 "validade 7 dias", "renovacao manual"
- **Detalhes**: `Active(0)`, `Expired(1)`, `Regenerated(2)`. `Expired` e setado pelo job recorrente. `Regenerated` marca versoes anteriores apos rotacao — mantidas historicamente mas nao aceitam mais cadastros.

### 10. Domain events do aggregate `AccountancyInviteLink`

- **Camada**: Domain
- **Tipo**: DomainEvent
- **Acao**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Domain/Events/DomainEvents.cs`
- **Depende de**: #9
- **Spec-ref**: §5 "link sempre visivel", §7 "apos 7 dias", §9
- **Detalhes**: Quatro records:
  - `InviteLinkGenerated(Guid Id, Guid AccountancyId, string Token, string TokenHash, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, ulong Version)` — role fixo `AccountingEmployee` nao entra no evento (invariante do aggregate).
  - `InviteLinkRegenerated(Guid Id, string Token, string TokenHash, DateTimeOffset RegeneratedAt, DateTimeOffset ExpiresAt, ulong Version)` — reseta janela de 7 dias.
  - `InviteLinkUsed(Guid Id, Guid AccountancyId, Guid UsedByUserId, string UsedByEmail, int UsageCount, DateTimeOffset UsedAt, ulong Version)` — usado tanto no registro novo quanto no join de usuario existente.
  - `InviteLinkExpired(Guid Id, DateTimeOffset ExpiredAt, ulong Version)` — disparado pelo job recorrente.

### 11. Aggregate `AccountancyInviteLink`

- **Camada**: Domain
- **Tipo**: Aggregate
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Domain/Aggregates/AccountancyInviteLink.cs`
- **Depende de**: #9, #10
- **Spec-ref**: §4 cenario 3, §5, §7
- **Detalhes**: Stream unico por `AccountancyId`. Id = GUIDv5 deterministico sob namespace fixo — tentativa de `Create` duplicado falha no event store por conflito de versao. Metodos:
  - `static Create(Guid accountancyId, string rawToken, string tokenHash, DateTimeOffset now)` — raise `InviteLinkGenerated(ExpiresAt = now + 7 days)`.
  - `Regenerate(string newRawToken, string newTokenHash, DateTimeOffset now)` — so permitido se `Status in {Active, Expired}`. Senao `ConflictError`. Reseta janela.
  - `RegisterUsage(Guid userId, string email, DateTimeOffset usedAt)` — so permitido se `Status == Active && ExpiresAt > usedAt`. Incrementa `UsageCount`, raise `InviteLinkUsed`.
  - `Expire(DateTimeOffset expiredAt)` — idempotente; so raise se `Status == Active && ExpiresAt <= expiredAt`. Raise `InviteLinkExpired`.
  - Campo `RawToken` em memoria (para poder retornar no response sem ir na projection) — apenas na projection o token persiste tambem.

### 12. Projection model `AccountancyInviteLink`

- **Camada**: Domain
- **Tipo**: ProjectionModel
- **Acao**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Domain/Projection/ProjectionModel.cs`
- **Depende de**: #9
- **Spec-ref**: §5 "copiar com botao", §9 "metadado gerado em/expira em/N pessoas"
- **Detalhes**: Record `AccountancyInviteLink(Guid Id, Guid AccountancyId, string Token, string TokenHash, int UsageCount, string Status, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, DateTimeOffset? LastUsedAt, DateTimeOffset? UpdatedAt, bool IsDeleted)`. **Raw `Token` persistido** — o botao "Copiar" precisa devolver o mesmo token apos F5; ameaca de forca bruta e mitigada por 256 bits de entropia.

### 13. EF configurations — StoreEvent + Snapshot

- **Camada**: Persistence
- **Tipo**: EFConfiguration
- **Acao**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Persistence/Configurations/StoreEventConfiguration.cs` + `SnapshotConfiguration.cs`
- **Depende de**: #11
- **Spec-ref**: infra obrigatoria
- **Detalhes**: Classes vazias `AccountancyInviteLinkStoreEventConfiguration : StoreEventConfiguration<AccountancyInviteLink>;` e idem para snapshot.

### 14. Migration — `AddAccountancyInviteLinkAggregate`

- **Camada**: Persistence
- **Tipo**: Migration
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Persistence/Migrations/*_AddAccountancyInviteLinkAggregate.cs`
- **Depende de**: #13
- **Spec-ref**: infra
- **Detalhes**: `dotnet ef migrations add AddAccountancyInviteLinkAggregate -c IdentityDbContext`. Cria `AccountancyInviteLinkStoreEvents` e `AccountancyInviteLinkSnapshots` no schema `identity`.

### 15. Service `IInviteLinkTokenService` + impl

- **Camada**: Application (contrato) + Infrastructure (impl)
- **Tipo**: Service
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/Services/IInviteLinkTokenService.cs` + `Identity.Infrastructure/Services/InviteLinkTokenService.cs`
- **Depende de**: nenhum
- **Spec-ref**: §9 (formato do token)
- **Detalhes**: `string GenerateRawToken()` (32 bytes `RandomNumberGenerator` → base64url sem padding) + `string HashToken(string raw)` (SHA-256 → base64). Registrado via Scrutor.

### 16. Projection handler `AccountancyInviteLink`

- **Camada**: Application
- **Tipo**: ProjectionHandler
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Events/ProjectionAccountancyInviteLinkEventHandler.cs`
- **Depende de**: #10, #12
- **Spec-ref**: read-side do link
- **Detalhes**: Composite interface com os 4 eventos. `Generated` insere; `Regenerated` substitui token/hash e reseta `Status=Active`, `ExpiresAt`; `Used` incrementa `UsageCount` + `LastUsedAt`; `Expired` muda `Status=Expired`. Idempotencia + rethrow via `InvalidOperationException` para retry MassTransit.

### 17. Consumer `ProjectionAccountancyInviteLinkConsumer`

- **Camada**: Web
- **Tipo**: ConsumerRegistration
- **Acao**: criar
- **Caminho**: `src/Web/ServiceInstallers/EventBus/Consumers/IdentityModule/ProjectionAccountancyInviteLinkConsumer.cs`
- **Depende de**: #10, #16
- **Spec-ref**: read-side
- **Detalhes**: Implementa os 4 `IConsumer<...>` delegando ao handler.

### 18. `ConsumerConfiguration` — 4 novos endpoints

- **Camada**: Web
- **Tipo**: ConsumerRegistration
- **Acao**: modificar
- **Caminho**: `src/Web/ServiceInstallers/EventBus/Consumers/IdentityModule/ConsumerConfiguration.cs`
- **Depende de**: #17
- **Spec-ref**: obrigatorio
- **Detalhes**: 4 `ConfigureEventReceiveEndpoint` — um por evento do link.

### 19. Registro em `AddEventInteractors`

- **Camada**: Application
- **Tipo**: ConsumerRegistration
- **Acao**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Application/DependencyInjection/ServiceCollectionExtensions.cs`
- **Depende de**: #16
- **Spec-ref**: obrigatorio
- **Detalhes**: `AddScoped<IProjectionAccountancyInviteLinkEventHandler, ProjectionAccountancyInviteLinkEventHandler>()`.

---

### Grupo C — Commands, Queries, Response DTOs, Handlers

### 20. Response DTOs

- **Camada**: Shared
- **Tipo**: ResponseDTO
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Response/InviteLinkResponse.cs`
- **Depende de**: nenhum
- **Spec-ref**: §5, §9
- **Detalhes**: Quatro records no mesmo arquivo:
  - `InviteLinkResponse(Guid Id, Guid AccountancyId, string Url, int UsageCount, DateTimeOffset? LastUsedAt, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, string Status)` — `Url = {FrontendBaseUrl}/join/{token}`.
  - `InviteLinkInfoResponse(Guid AccountancyId, string AccountancyName)` — pagina publica so precisa do nome (role e invariante).
  - `UserInviteCreatedResponse(Guid Id, string InviteLink)` — para SystemAdmin copiar o link.
  - `RegisterByInviteLinkResponse(Guid? UserId, bool RequiresAuthentication, string? Email)` — `RequiresAuthentication=true` quando email ja existe; frontend redireciona para login com token preservado.

### 21. Commands do link

- **Camada**: Shared
- **Tipo**: Command
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Commands/{RegenerateInviteLinkCommand,RegisterByInviteLinkCommand,JoinByInviteLinkCommand,RemoveMemberCommand}.cs`
- **Depende de**: #20
- **Spec-ref**: §4, §9, §6 (remocao)
- **Detalhes**:
  - `RegenerateInviteLinkCommand(Guid AccountancyId) : ICommand<InviteLinkResponse>`.
  - `RegisterByInviteLinkCommand(string Token, string FirstName, string LastName, string Email, string Password, string ConfirmPassword) : ICommand<RegisterByInviteLinkResponse>`.
  - `JoinByInviteLinkCommand(string Token) : ICommand` — executado por user autenticado; adiciona membership apos login.
  - `RemoveMemberCommand(Guid AccountancyId, Guid UserId) : ICommand` — AccountingAdmin remove funcionario.

### 22. Queries do link

- **Camada**: Shared
- **Tipo**: Query
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Queries/{GetInviteLinkByAccountancyQuery,GetInviteLinkInfoQuery}.cs`
- **Depende de**: #20
- **Spec-ref**: §5, §4
- **Detalhes**:
  - `GetInviteLinkByAccountancyQuery(Guid AccountancyId) : IQuery<InviteLinkResponse>` — lazy-create se nao existe.
  - `GetInviteLinkInfoQuery(string Token) : IQuery<InviteLinkInfoResponse>`.

### 23. Validators

- **Camada**: Application
- **Tipo**: Validator
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Validators/{Regenerate,RegisterByInviteLink,JoinByInviteLink,RemoveMember}Validator.cs`
- **Depende de**: #21
- **Spec-ref**: §9
- **Detalhes**:
  - `RegenerateInviteLinkValidator` — `AccountancyId` nao vazio.
  - `RegisterByInviteLinkValidator` — token, nome/sobrenome (≤100), email valido, senha (politica existente), `ConfirmPassword == Password`.
  - `JoinByInviteLinkValidator` — token nao vazio.
  - `RemoveMemberValidator` — `AccountancyId` + `UserId` nao vazios, diferentes de `Guid.Empty`.

### 24. Handler `GetInviteLinkByAccountancyHandler`

- **Camada**: Application
- **Tipo**: Handler
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Queries/GetInviteLinkByAccountancyHandler.cs`
- **Depende de**: #11, #15, #20, #22
- **Spec-ref**: §5 "sempre visivel com botao copiar"
- **Detalhes**: Resolve Id deterministico via GUIDv5. `LoadAggregateAsync` — se `NotFound`, lazy-create: gera token+hash, `AccountancyInviteLink.Create`, `AppendEventsAsync`, monta response em memoria. Se existe e `Status != Active` ou `ExpiresAt <= now`, response ainda retorna (frontend exibe "Expirado — Renovar"). Monta URL via `FrontendOptions.BaseUrl`.

### 25. Handler `RegenerateInviteLinkHandler`

- **Camada**: Application
- **Tipo**: Handler
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Commands/RegenerateInviteLinkHandler.cs`
- **Depende de**: #11, #15, #21
- **Spec-ref**: §9 "renovacao manual"
- **Detalhes**: Resolve Id deterministico. `LoadAggregateAsync` — se nao existe, comporta-se como `Create`. Senao `Regenerate(newToken, newHash, now)` (que rejeita se `Status == Regenerated`). Append + response.

### 26. Handler `GetInviteLinkInfoHandler`

- **Camada**: Application
- **Tipo**: Handler
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Queries/GetInviteLinkInfoHandler.cs`
- **Depende de**: #12, #15, #22
- **Spec-ref**: §5 "pagina de registro exibe nome da contabilidade"
- **Detalhes**: Hashea token. Busca projection `TokenHash == hash && Status == Active && ExpiresAt > now`. Se nao achar → `NotFoundError` (mensagem generica, nao vaza se expirou vs inexistente). Injeta `IAccountancyProjection<ProjectionModel.Accountancy>` para carregar nome (leitura cross-module — fronteira unica). Retorna `InviteLinkInfoResponse`.

### 27. Handler `RegisterByInviteLinkHandler`

- **Camada**: Application
- **Tipo**: Handler
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Commands/RegisterByInviteLinkHandler.cs`
- **Depende de**: #3, #11, #15, #21
- **Spec-ref**: §4 cenario 3, §9 "email ja cadastrado em outra contabilidade"
- **Detalhes**:
  1. Hashea token; busca projection `Status == Active && ExpiresAt > now`. Nao achou → `NotFoundError` ("Link.NotFoundOrExpired").
  2. Busca `User` por email na projection.
  3. **Se existe** → retorna `RegisterByInviteLinkResponse(UserId: null, RequiresAuthentication: true, Email: email)`. Nenhum write. Frontend redireciona para login com o mesmo token; ao voltar autenticado, chama `JoinByInviteLink`.
  4. **Se nao existe** → hash password, `User.Create(link.Id, firstName, lastName, email, passwordHash, role: AccountingEmployee, accountancyId: link.AccountancyId)`; load `AccountancyInviteLink`; `link.RegisterUsage(user.Id, email, now)`; append dos dois aggregates. Retorna `RegisterByInviteLinkResponse(UserId: user.Id, RequiresAuthentication: false, Email: null)`.

### 28. Handler `JoinByInviteLinkHandler`

- **Camada**: Application
- **Tipo**: Handler
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Commands/JoinByInviteLinkHandler.cs`
- **Depende de**: #3, #11, #15, #21
- **Spec-ref**: §9 "apos autenticar, usuario e adicionado automaticamente"
- **Detalhes**: Executado por usuario autenticado. Resolve `UserId` via `ICurrentUserService`. Hashea token, valida link `Active && ExpiresAt > now`. `LoadAggregateAsync<User>(currentUserId)`. Chama `user.JoinAccountancy(link.AccountancyId, AccountingEmployee, now)` (idempotente). Carrega `AccountancyInviteLink`, `link.RegisterUsage(user.Id, user.Email, now)`. Append.

### 29. Handler `RemoveMemberHandler`

- **Camada**: Application
- **Tipo**: Handler
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Commands/RemoveMemberHandler.cs`
- **Depende de**: #3, #21
- **Spec-ref**: §6 "remocao de membros"
- **Detalhes**: Injeta `ICurrentUserService`. Regras:
  1. Current user precisa ser `AccountingAdmin` da `cmd.AccountancyId` — checa via projection `User`.
  2. Nao pode remover a si mesmo → `ConflictError` ("User.CannotRemoveSelf").
  3. Load `User(cmd.UserId)`; se nao tem membership na accountancy → `NotFoundError`.
  4. Se membership tem `Role == AccountingAdmin` → `ConflictError` ("User.CannotRemoveAdmin") — spec §4 "cada contabilidade tem um unico AccountingAdmin criado pelo SysAdmin".
  5. `user.LeaveAccountancy(cmd.AccountancyId, now)`. Append.

---

### Grupo D — Job recorrente de expiracao

### 30. Command interno `ExpireInviteLinksCommand` + handler

- **Camada**: Application
- **Tipo**: Handler (+ RecurringJob)
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Jobs/ExpireInviteLinksJob.cs`
- **Depende de**: #11, #12
- **Spec-ref**: §9 "apos 7 dias link para de aceitar cadastros"
- **Detalhes**: `internal sealed record ExpireInviteLinksCommand() : ICommand`. Handler busca projection `Status == Active && ExpiresAt < now`. Para cada: `LoadAggregateAsync<AccountancyInviteLink>`, `Expire(now)`, `AppendEventsAsync`. Log warning + `continue` em caso de falha de item individual.

### 31. Registro em `RegisterRecurringJobsStartupTask`

- **Camada**: Web
- **Tipo**: RecurringJob
- **Acao**: modificar
- **Caminho**: `src/Web/StartupTasks/RegisterRecurringJobsStartupTask.cs`
- **Depende de**: #30
- **Spec-ref**: obrigatorio
- **Detalhes**: `jobScheduler.ScheduleRecurring("expire-invite-links", "0 * * * *", new ExpireInviteLinksCommand())` — mesmo cron dos invites (`0 * * * *`, horario).

---

### Grupo E — Web (rotas, tags, endpoints)

### 32. Rotas `IdentityRoutes`

- **Camada**: Web
- **Tipo**: Endpoint (routes)
- **Acao**: modificar
- **Caminho**: `src/Web/Endpoints/Routes/IdentityRoutes.cs`
- **Depende de**: nenhum
- **Spec-ref**: §4, §5
- **Detalhes**: Adicionar constantes:
  - `AccountancyId = "accountancyId"`, `UserId = "userId"`.
  - `GetInviteLink = $"{BaseUri}/accountancies/{{{AccountancyId}:guid}}/invite-link"`.
  - `RegenerateInviteLink = $"{BaseUri}/accountancies/{{{AccountancyId}:guid}}/invite-link/regenerate"`.
  - `GetInviteLinkInfo = $"{BaseUri}/invite-links/info"` (token via query).
  - `RegisterByInviteLink = $"{BaseUri}/invite-links/register"`.
  - `JoinByInviteLink = $"{BaseUri}/invite-links/join"`.
  - `RemoveMember = $"{BaseUri}/accountancies/{{{AccountancyId}:guid}}/members/{{{UserId}:guid}}"`.

### 33. Tags

- **Camada**: Web
- **Tipo**: Endpoint (tags)
- **Acao**: modificar
- **Caminho**: `src/Web/Endpoints/Tags.cs`
- **Depende de**: nenhum
- **Spec-ref**: Swagger
- **Detalhes**: `Tags.Identity.InviteLinks = "Identity - Invite Links"`. `Tags.Identity.Members = "Identity - Members"`.

### 34. Endpoint `GetInviteLinkByAccountancyEndpoint`

- **Camada**: Web
- **Tipo**: Endpoint
- **Acao**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/InviteLinks/GetInviteLinkByAccountancyEndpoint.cs`
- **Depende de**: #24, #32, #33
- **Spec-ref**: §5
- **Detalhes**: `[HttpGet(IdentityRoutes.GetInviteLink)]`, `[Authorize(Roles = "AccountingAdmin")]`. `[FromRoute] Guid AccountancyId`.

### 35. Endpoint `RegenerateInviteLinkEndpoint`

- **Camada**: Web
- **Tipo**: Endpoint
- **Acao**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/InviteLinks/RegenerateInviteLinkEndpoint.cs`
- **Depende de**: #25, #32, #33
- **Spec-ref**: §9
- **Detalhes**: `[HttpPost(IdentityRoutes.RegenerateInviteLink)]`, `[Authorize(Roles = "AccountingAdmin")]`.

### 36. Endpoint `GetInviteLinkInfoEndpoint`

- **Camada**: Web
- **Tipo**: Endpoint
- **Acao**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/InviteLinks/GetInviteLinkInfoEndpoint.cs`
- **Depende de**: #26, #32, #33
- **Spec-ref**: §5 "pagina de registro exibe nome"
- **Detalhes**: `[HttpGet(IdentityRoutes.GetInviteLinkInfo)]`, `[AllowAnonymous]`. `[FromQuery] string Token`.

### 37. Endpoint `RegisterByInviteLinkEndpoint`

- **Camada**: Web
- **Tipo**: Endpoint
- **Acao**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/InviteLinks/RegisterByInviteLinkEndpoint.cs`
- **Depende de**: #27, #32, #33
- **Spec-ref**: §4 cenario 3, §9
- **Detalhes**: `[HttpPost(IdentityRoutes.RegisterByInviteLink)]`, `[AllowAnonymous]`. Retorna `RegisterByInviteLinkResponse` (pode ser 200 OK com `RequiresAuthentication=true` para indicar redirect).

### 38. Endpoint `JoinByInviteLinkEndpoint`

- **Camada**: Web
- **Tipo**: Endpoint
- **Acao**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/InviteLinks/JoinByInviteLinkEndpoint.cs`
- **Depende de**: #28, #32, #33
- **Spec-ref**: §9 "apos autenticar"
- **Detalhes**: `[HttpPost(IdentityRoutes.JoinByInviteLink)]`, `[Authorize]`. `[FromBody] { string Token }`.

### 39. Endpoint `RemoveMemberEndpoint`

- **Camada**: Web
- **Tipo**: Endpoint
- **Acao**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/Members/RemoveMemberEndpoint.cs`
- **Depende de**: #29, #32, #33
- **Spec-ref**: §6 "remocao de membros"
- **Detalhes**: `[HttpDelete(IdentityRoutes.RemoveMember)]`, `[Authorize(Roles = "AccountingAdmin")]`.

---

### Grupo F — SystemAdmin flows (cenarios 1 e 2)

### 40. Modificar `InviteUserCommand` — trocar response

- **Camada**: Shared
- **Tipo**: Command
- **Acao**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Commands/InviteUserCommand.cs`
- **Depende de**: #20
- **Spec-ref**: §5 "link aparece na tela apos criacao"
- **Detalhes**: `ICommand<IdentifierResponse>` → `ICommand<UserInviteCreatedResponse>`. Assinatura (`Email, Role, AccountancyId`) inalterada.

### 41. Modificar `InviteUserHandler` — retornar link

- **Camada**: Application
- **Tipo**: Handler
- **Acao**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Commands/InviteUserHandler.cs`
- **Depende de**: #40
- **Spec-ref**: §5
- **Detalhes**: Monta `inviteLink = $"{FrontendBaseUrl}/accept-invite/{rawToken}"` (ja faz). Retorna `new UserInviteCreatedResponse(invite.Id, inviteLink)`. Email continua via Hangfire.

### 42. Modificar `InviteUserEndpoint` — restringir + novo response type

- **Camada**: Web
- **Tipo**: Endpoint
- **Acao**: modificar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/InviteUserEndpoint.cs`
- **Depende de**: #41
- **Spec-ref**: §4 cenarios 1+2 "exclusivo SystemAdmin"
- **Detalhes**: `.WithActionResult<IdentifierResponse>` → `.WithActionResult<UserInviteCreatedResponse>`. `[Authorize(Roles = "SystemAdmin")]`. `[ProducesResponseType<UserInviteCreatedResponse>(200)]`.

---

## Checklist de registro

- [x] **`ConsumerConfiguration.cs` (Identity)** — itens #8 e #18. Adicionar 2 endpoints de User (`UserMembershipAdded`, `UserMembershipRemoved`) + 4 endpoints de link (`InviteLinkGenerated`, `InviteLinkRegenerated`, `InviteLinkUsed`, `InviteLinkExpired`).
- [x] **`AddEventInteractors()` (Identity)** — itens #5 (`ProjectionUserEventHandler` ja registrado — novos metodos caem na registracao existente) e #19 (`ProjectionAccountancyInviteLinkEventHandler`).
- [x] **`RegisterRecurringJobsStartupTask`** — item #31. Adicionar `ScheduleRecurring("expire-invite-links", "0 * * * *", new ExpireInviteLinksCommand())`.
- [ ] **`MigrateDatabaseStartupTask`** — nao tocar. Sem novo DbContext.
- [ ] **`PersistenceServiceInstaller`** — nao tocar. `IIdentityProjection<>` ja registra o generic que cobre `AccountancyInviteLink`.
- [x] **`InfrastructureServiceInstaller` (Identity)** — item #15. Scrutor cobre `InviteLinkTokenService`.
- [x] **Rebuild da projection `User`** — item #6. Procedimento operacional documentado (drop + replay) ou migracao incremental.
- [x] **JWT claims** — **pendente**: hoje o JWT provavelmente carrega `accountancy_id` + `role` escalares. Com multi-membership, o claim precisa ser revisto (array de memberships ou "accountancy ativa" via seleção de login — fora do escopo da spec mas o backend precisa decidir o shape agora). **Ver Riscos**.

---

## Riscos e pontos de atencao

- **Refactor multi-membership no aggregate `User`** — e o maior risco do plano. Muda shape do projection model (`User`), exige rebuild da projection apos deploy, potencialmente afeta JWT claims e qualquer endpoint/handler que le `User.AccountancyId`/`Role` escalares. Itens #1–#8 cobrem o domain + projection; **uma varredura completa de `user.AccountancyId` no codigo antes da implementacao e obrigatoria** para mapear todos os callsites (Accountancy, WorkflowTemplate, `ICurrentUserService`, geracao de JWT). Se a superficie de mudanca for maior que o esperado, vale considerar **Option C** (aggregate `UserMembership` separado) para reduzir blast radius.
- **JWT claims com multi-membership** — spec §7 explicitamente deixa "selecao de contabilidade ativa no login" fora do escopo. Mas o backend precisa decidir **agora** se o JWT atual continua carregando um unico `accountancy_id`/`role` (e a selecao e feita em `POST /auth/login/select-accountancy` futuro) ou se o token ja carrega uma lista. Proposta MVP: manter claim escalar apontando para a **primary membership** (primeira joined), e exigir login futuro para trocar de contabilidade. **Confirmar com o humano antes da implementacao**.
- **Rebuild da projection User (item #6)** — down-time minimo ou zero? Alternativas: (a) dropar collection + replay completo (rapido mas pageview vazia durante rebuild); (b) migracao incremental por codigo. MVP pragmatico: (a) num horario de baixo trafego. Documentar como operational runbook.
- **GUIDv5 deterministico (item #11)** — pattern novo no repo. Justificar em comentario no aggregate. Namespace fixo guardado como `const Guid` dentro do aggregate.
- **Raw token persistido na projecao (item #12)** — decisao de produto (botao "Copiar" + metadado publico). 256 bits de entropia mitigam forca bruta. XML doc do campo deve declarar. Sanitizar token em logs.
- **RegisterByInviteLink idempotencia e race** (item #27) — dois POSTs simultaneos com o mesmo email e token novo: event store rejeita o segundo por conflito de versao no `User` (id gerado antes). Aceitar essa forma de proteção; o 2º POST recebe 409 e o cliente trata.
- **Remocao do AccountingAdmin (item #29)** — regra §4 "contabilidade tem um unico AccountingAdmin criado pelo SysAdmin" implica que AccountingAdmin nao se remove nem remove outro admin. MVP: `RemoveMember` rejeita remover membership com role `AccountingAdmin` — so SysAdmin podera trocar o admin de uma contabilidade (fora do escopo desta spec).
- **Cross-module read para nome da contabilidade (item #26)** — `IAccountancyProjection<Accountancy>` usado por Identity.Application. Unica passagem; leitura pura; sem acoplamento de escrita. Pattern aceitavel. TODO futuro: replicar nome da accountancy dentro do doc do link para eliminar a dependencia cross-module no hot path de registro publico.
- **Breaking change em `InviteUserCommand` (item #40)** — response shape muda de `IdentifierResponse` para `UserInviteCreatedResponse`. Frontend atual do painel SystemAdmin consome — coordenar deploy.
- **Convention tests** — `tests/Web.Tests` valida Swagger em portugues + `OperationId` + `Tags`. Os **6 endpoints novos** (#34-#39) + endpoint modificado (#42) precisam atender.
- **Spec §7 — notificacoes fora do escopo** — `InviteLinkUsed` e publicado mas nao consumido nesta spec. O evento fica disponivel para um consumer futuro sem retrabalho.
- **`User.Create` — slot `inviteId`** — quando o registro vem pelo link, passamos `link.Id` no parametro `inviteId`. Manter o parametro com esse nome para nao criar breaking change no aggregate. XML doc deve explicitar que o slot significa "convite por email **ou** link".
- **Spec §6 — "email ja cadastrado em outra contabilidade" quando o registro e novo (nao existente)** — cenario: email aparece na projection mas nao tem membership na accountancy do link. MVP trata igual a "email existe" → retorna `RequiresAuthentication=true`. Se o humano quiser tratar "email existe mas nunca fez login" (invite email pending) de forma diferente, flag agora.
