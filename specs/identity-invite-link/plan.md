# Plano — identity-invite-link

**Spec**: specs/identity-invite-link/spec.md @ product.societiza
**Modulos afetados**: Identity (extensao), Notifications (novo), Web
**Modulo novo?**: sim — Notifications (5 projetos: Domain, Application, Infrastructure, Persistence, Shared)
**Resumo**: Adiciona dois links permanentes de convite por contabilidade (AccountingAdmin + AccountingEmployee), com pagina publica de registro e notificacao in-app via SignalR para avisar o admin quando alguem entra pelo link. O fluxo por email existente continua, mas restrito a SystemAdmin e com o link de aceite retornado na resposta. Cria o modulo Notifications — primeiro consumidor cross-module do Identity e primeira introducao de SignalR no projeto.

---

## Pre-condicoes

1. **Um usuario pertence a uma unica contabilidade** — mantem `User.AccountancyId` escalar. Email duplicado (mesmo em outra contabilidade) retorna 409. Decisao MVP confirmada com o humano — multi-membership fica para depois.
2. **Sem endpoint orquestrador accountancy+invite** — o frontend compoe as duas chamadas existentes (criar accountancy + convidar admin). Nao entra no escopo desta spec.
3. **`InviteUserEndpoint` (email) passa a ser restrito a SystemAdmin** — usado apenas pela ferramenta interna para colocar o primeiro admin da contabilidade. O link generico nao substitui este caminho; ele e complementar.
4. **Remocao de membros nao entra no MVP** — spec foca em onboarding. `Revoke`/`Remove` de usuarios ativos fica fora.
5. **`FrontendOptions.BaseUrl` ja existe e e reutilizado** — o invite link (`/join/{token}`) e o accept-invite (`/accept-invite/{token}`) usam o mesmo binding. Nao cria `InviteLinkOptions` separado — evita duplicacao.

---

## Itens

### 1. SmartEnum `InviteLinkStatus`

- **Camada**: Domain
- **Tipo**: SmartEnum
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Domain/Enumerations/InviteLinkStatus.cs`
- **Depende de**: nenhum
- **Spec-ref**: §3 "Estado do link"
- **Detalhes**: `Active(0)`, `Regenerated(1)`. Regenerated marca links antigos apos rotacao — lookup por token so considera `Active`. Segue pattern existente (implicit conversions string/int, `[method: JsonConstructor]`, private sealed subclasses).

### 2. Domain Events do aggregate `AccountancyInviteLink`

- **Camada**: Domain
- **Tipo**: DomainEvent
- **Acao**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Domain/Events/DomainEvents.cs`
- **Depende de**: #1
- **Spec-ref**: §3 "Geracao/regeneracao do link", §4 "Registro por link"
- **Detalhes**: Adicionar tres records em `DomainEvents`:
  - `InviteLinkGenerated(Guid Id, Guid AccountancyId, string Role, string Token, string TokenHash, string Status, DateTimeOffset CreatedAt, ulong Version)` — `Role` e `Status` serializados como string via `.Name`.
  - `InviteLinkRegenerated(Guid Id, string Token, string TokenHash, DateTimeOffset RegeneratedAt, ulong Version)`
  - `InviteLinkUsed(Guid Id, Guid AccountancyId, string Role, Guid UsedByUserId, string UsedByEmail, int UsageCount, DateTimeOffset UsedAt, ulong Version)` — `UsedByUserId` + `UsedByEmail` alimentam a notificacao do modulo Notifications.

### 3. Aggregate `AccountancyInviteLink`

- **Camada**: Domain
- **Tipo**: Aggregate
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Domain/Aggregates/AccountancyInviteLink.cs`
- **Depende de**: #1, #2
- **Spec-ref**: §3 "Aggregate de link generico"
- **Detalhes**: Aggregate event-sourced com stream unico por `(AccountancyId, Role)`. Id gerado por **GUIDv5 deterministico** a partir de `(AccountancyId, Role)` sob namespace fixo — garante que `Create` duplicado falhe por conflito de versao no event store (v0 ja existe). Metodos:
  - `static Create(Guid accountancyId, UserRole role, string rawToken, string tokenHash)` — levanta `InviteLinkGenerated`.
  - `Regenerate(string newRawToken, string newTokenHash)` — levanta `InviteLinkRegenerated`. Token antigo passa a nao resolver.
  - `RegisterUsage(Guid userId, string email, DateTimeOffset usedAt)` — incrementa `UsageCount`, levanta `InviteLinkUsed`.
  - `When(...)` para cada evento, hidrata `Status`/`Role` via `FromName`.

### 4. Projection Model `AccountancyInviteLink`

- **Camada**: Domain
- **Tipo**: ProjectionModel
- **Acao**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Domain/Projection/ProjectionModel.cs`
- **Depende de**: #1
- **Spec-ref**: §3 "Consulta e compartilhamento do link"
- **Detalhes**: Adicionar record no namespace `ProjectionModel`: `AccountancyInviteLink(Guid Id, Guid AccountancyId, string Role, string Token, string TokenHash, int UsageCount, string Status, DateTimeOffset CreatedAt, DateTimeOffset? LastUsedAt, DateTimeOffset? UpdatedAt, bool IsDeleted)`. **Raw `Token` persistido** (decisao de produto — link permanente compartilhado por WhatsApp; admin precisa copiar novamente depois de refresh). XML doc explicitando o motivo. `TokenHash` indexado para lookup no registro anonimo.

### 5. EF configuration — StoreEvent

- **Camada**: Persistence
- **Tipo**: EFConfiguration
- **Acao**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Persistence/Configurations/StoreEventConfiguration.cs`
- **Depende de**: #3
- **Spec-ref**: §3 (infraestrutura de aggregate novo)
- **Detalhes**: Adicionar classe vazia `public class AccountancyInviteLinkStoreEventConfiguration : StoreEventConfiguration<AccountancyInviteLink>;`. Gera tabela `identity."AccountancyInviteLinkStoreEvents"` automaticamente.

### 6. EF configuration — Snapshot

- **Camada**: Persistence
- **Tipo**: EFConfiguration
- **Acao**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Persistence/Configurations/SnapshotConfiguration.cs`
- **Depende de**: #3
- **Spec-ref**: §3 (infraestrutura de aggregate novo)
- **Detalhes**: Adicionar classe vazia `public class AccountancyInviteLinkSnapshotConfiguration : SnapshotConfiguration<AccountancyInviteLink>;`. Gera tabela `identity."AccountancyInviteLinkSnapshots"`.

### 7. Migration — AddAccountancyInviteLinkAggregate

- **Camada**: Persistence
- **Tipo**: Migration
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Persistence/Migrations/*_AddAccountancyInviteLinkAggregate.cs`
- **Depende de**: #5, #6
- **Spec-ref**: §3
- **Detalhes**: `dotnet ef migrations add AddAccountancyInviteLinkAggregate --project src/Modules/Identity/Identity.Persistence --startup-project src/Web --context IdentityDbContext`. Cria as duas tabelas no schema `identity`.

### 8. Response DTOs `InviteLinkResponse`, `InviteLinksResponse`, `InviteLinkInfoResponse`, `UserInviteCreatedResponse`

- **Camada**: Shared
- **Tipo**: ResponseDTO
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Response/InviteLinkResponse.cs`
- **Depende de**: nenhum
- **Spec-ref**: §3 "Consulta do link", §4 "Registro", §2 "Email-based ajustado"
- **Detalhes**: Quatro sealed records num mesmo arquivo:
  - `InviteLinkResponse(Guid Id, Guid AccountancyId, string Role, string Url, int UsageCount, DateTimeOffset? LastUsedAt, DateTimeOffset CreatedAt)` — `Url` ja montada `{FrontendBaseUrl}/join/{token}`.
  - `InviteLinksResponse(InviteLinkResponse AdminLink, InviteLinkResponse EmployeeLink)` — resposta do `GET /invite-links` (ambos os roles).
  - `InviteLinkInfoResponse(Guid AccountancyId, string AccountancyName, string Role)` — consumido pela pagina publica de registro.
  - `UserInviteCreatedResponse(Guid Id, string InviteLink)` — resposta do email invite (#26) para SystemAdmin copiar o link.

### 9. Command `RegenerateInviteLinkCommand`

- **Camada**: Shared
- **Tipo**: Command
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Commands/RegenerateInviteLinkCommand.cs`
- **Depende de**: #8
- **Spec-ref**: §3 "Rotacao do link"
- **Detalhes**: `sealed record RegenerateInviteLinkCommand(Guid AccountancyId, string Role) : ICommand<InviteLinkResponse>`. `Role` como string — validator resolve via `UserRole.TryFromName`.

### 10. Command `RegisterByInviteLinkCommand`

- **Camada**: Shared
- **Tipo**: Command
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Commands/RegisterByInviteLinkCommand.cs`
- **Depende de**: nenhum
- **Spec-ref**: §4 "Registro anonimo via link"
- **Detalhes**: `sealed record RegisterByInviteLinkCommand(string Token, string FirstName, string LastName, string Email, string Password, string ConfirmPassword) : ICommand<IdentifierResponse>`. Token raw (base64url) — o handler hashea antes do lookup.

### 11. Query `GetInviteLinksByAccountancyQuery`

- **Camada**: Shared
- **Tipo**: Query
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Queries/GetInviteLinksByAccountancyQuery.cs`
- **Depende de**: #8
- **Spec-ref**: §3 "Admin consulta os links"
- **Detalhes**: `sealed record GetInviteLinksByAccountancyQuery(Guid AccountancyId) : IQuery<InviteLinksResponse>`. Retorna os dois links (Admin e Employee) — lazy-create os faltantes.

### 12. Query `GetInviteLinkInfoQuery`

- **Camada**: Shared
- **Tipo**: Query
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Queries/GetInviteLinkInfoQuery.cs`
- **Depende de**: #8
- **Spec-ref**: §4 "Pagina publica de registro carrega o nome da contabilidade"
- **Detalhes**: `sealed record GetInviteLinkInfoQuery(string Token) : IQuery<InviteLinkInfoResponse>`. Consumido anonimamente pela pagina publica.

### 13. Service `IInviteLinkTokenService` + impl

- **Camada**: Application
- **Tipo**: Service
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/Services/IInviteLinkTokenService.cs` + impl em `Identity.Infrastructure/Services/InviteLinkTokenService.cs`
- **Depende de**: nenhum
- **Spec-ref**: §3 "Formato do token"
- **Detalhes**: `string GenerateRawToken()` (32 bytes `RandomNumberGenerator` → base64url sem padding) e `string HashToken(string raw)` (SHA-256 → base64). Centraliza o formato — nenhum handler implementa isso inline. Registro via Scrutor no `InfrastructureServiceInstaller` existente.

### 14. Validator `RegenerateInviteLinkValidator`

- **Camada**: Application
- **Tipo**: Validator
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Validators/RegenerateInviteLinkValidator.cs`
- **Depende de**: #9
- **Spec-ref**: §3 "Rotacao"
- **Detalhes**: `AccountancyId` nao vazio. `Role` deve resolver para `AccountingAdmin` ou `AccountingEmployee` — `SystemAdmin` nao tem link generico.

### 15. Validator `RegisterByInviteLinkValidator`

- **Camada**: Application
- **Tipo**: Validator
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Validators/RegisterByInviteLinkValidator.cs`
- **Depende de**: #10
- **Spec-ref**: §4 "Formulario da pagina publica"
- **Detalhes**: `Token` nao vazio. `FirstName`/`LastName` nao vazios e <= 100. `Email` valido <= 150. `Password` >= 8 chars com politica minima (ja existente no projeto). `ConfirmPassword == Password`.

### 16. Handler `RegenerateInviteLinkHandler`

- **Camada**: Application
- **Tipo**: Handler
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Commands/RegenerateInviteLinkHandler.cs`
- **Depende de**: #3, #9, #13
- **Spec-ref**: §3 "Rotacao do link"
- **Detalhes**: Resolve `Id` do aggregate via GUIDv5 `(AccountancyId, Role)`. `LoadAggregateAsync<AccountancyInviteLink>` — se nao existir, retorna `NotFoundError`. Gera novo raw token + hash via `IInviteLinkTokenService`, chama `link.Regenerate`. Append events. Retorna `InviteLinkResponse` (com URL ja montada via `FrontendOptions.BaseUrl`).

### 17. Handler `GetInviteLinksByAccountancyHandler`

- **Camada**: Application
- **Tipo**: Handler
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Queries/GetInviteLinksByAccountancyHandler.cs`
- **Depende de**: #3, #11, #13
- **Spec-ref**: §3 "Admin consulta os links — lazy create"
- **Detalhes**: Para cada role (`AccountingAdmin`, `AccountingEmployee`): resolve Id deterministico, busca projection. Se existir → retorna. Se nao existir → gera token + hash, chama `AccountancyInviteLink.Create`, `AppendEventsAsync`, monta o Response do aggregate em memoria (projection sera atualizada async — nao bloqueia a resposta). Retorna `InviteLinksResponse(admin, employee)`.

### 18. Handler `GetInviteLinkInfoHandler`

- **Camada**: Application
- **Tipo**: Handler
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Queries/GetInviteLinkInfoHandler.cs`
- **Depende de**: #12, #13
- **Spec-ref**: §4 "Info publica"
- **Detalhes**: Hashea o token recebido, busca projection por `TokenHash == hash && Status == Active`. Se nao encontrar → `NotFoundError`. Injeta `IAccountancyProjection<ProjectionModel.Accountancy>` para ler o nome da contabilidade. Retorna `InviteLinkInfoResponse(AccountancyId, AccountancyName, Role)`.

### 19. Handler `RegisterByInviteLinkHandler`

- **Camada**: Application
- **Tipo**: Handler
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Commands/RegisterByInviteLinkHandler.cs`
- **Depende de**: #3, #10, #13
- **Spec-ref**: §4 "Registro anonimo"
- **Detalhes**: Fluxo completo:
  1. Hashea token, busca projection do link `Status == Active`. Nao achou → `NotFoundError` ("Link.NotFound").
  2. Checa email duplicado via `IIdentityProjection<User>` — qualquer match (mesmo em outra contabilidade) retorna `ConflictError` ("User.EmailAlreadyExists"). MVP single-accountancy.
  3. Load aggregate `AccountancyInviteLink` via `LoadAggregateAsync`. Hash password via `IPasswordService`.
  4. Chama `User.Create(inviteId: link.Id, firstName, lastName, email, passwordHash, role: UserRole.FromName(link.Role), accountancyId: link.AccountancyId)` — reutiliza o slot `inviteId` do aggregate User passando o Id do link generico (rastreabilidade).
  5. Chama `link.RegisterUsage(user.Id, email, now)`.
  6. Append events dos dois aggregates (ordem: User primeiro, depois link).
  7. Retorna `IdentifierResponse(user.Id)`.

### 20. Projection Handler `IProjectionAccountancyInviteLinkEventHandler` + impl

- **Camada**: Application
- **Tipo**: ProjectionHandler
- **Acao**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Events/ProjectionAccountancyInviteLinkEventHandler.cs`
- **Depende de**: #2, #4
- **Spec-ref**: §3, §4 (read-side do link)
- **Detalhes**: Interface composta `IProjectionAccountancyInviteLinkEventHandler : IEventHandler<InviteLinkGenerated>, IEventHandler<InviteLinkRegenerated>, IEventHandler<InviteLinkUsed>`. Classe implementa os tres `Handle`:
  - `InviteLinkGenerated` → `ReplaceInsertAsync(new(Id, AccountancyId, Role, Token, TokenHash, UsageCount: 0, Status: Active, CreatedAt, null, null, false))`.
  - `InviteLinkRegenerated` → `existing with { Token = ..., TokenHash = ..., Status = Active, UpdatedAt = ... }`. Se existia outro com mesmo Id, foi substituido (stream unico por `(AccountancyId, Role)`, nao cria novo documento).
  - `InviteLinkUsed` → `existing with { UsageCount = ev.UsageCount, LastUsedAt = ev.UsedAt, UpdatedAt = ev.UsedAt }`. Idempotente quando `existing` e null (log warning + return).
  - Todos os catches rethrow `InvalidOperationException` para MassTransit retry.

### 21. Registro do projection handler em `AddEventInteractors`

- **Camada**: Application
- **Tipo**: ConsumerRegistration
- **Acao**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Application/DependencyInjection/ServiceCollectionExtensions.cs`
- **Depende de**: #20
- **Spec-ref**: obrigatorio por pattern
- **Detalhes**: Adicionar `.AddScoped<IProjectionAccountancyInviteLinkEventHandler, ProjectionAccountancyInviteLinkEventHandler>()`.

### 22. Rotas `IdentityRoutes` — invite-links

- **Camada**: Web
- **Tipo**: Endpoint (routes)
- **Acao**: modificar
- **Caminho**: `src/Web/Endpoints/Routes/IdentityRoutes.cs`
- **Depende de**: nenhum
- **Spec-ref**: §3, §4 (URLs)
- **Detalhes**: Adicionar constantes:
  - `AccountancyId = "accountancyId"`, `Role = "role"` (parameter names)
  - `GetInviteLinks = $"{BaseUri}/accountancies/{{{AccountancyId}:guid}}/invite-links"`
  - `RegenerateInviteLink = $"{BaseUri}/accountancies/{{{AccountancyId}:guid}}/invite-links/{{{Role}}}/regenerate"`
  - `GetInviteLinkInfo = $"{BaseUri}/invite-links/info"` (token via query)
  - `RegisterByInviteLink = $"{BaseUri}/invite-links/register"`

### 23. Tag `Tags.Identity.InviteLinks`

- **Camada**: Web
- **Tipo**: Endpoint (tags)
- **Acao**: modificar
- **Caminho**: `src/Web/Endpoints/Tags.cs`
- **Depende de**: nenhum
- **Spec-ref**: Swagger grouping
- **Detalhes**: Adicionar constante `InviteLinks = "Identity - Invite Links"` dentro do grupo Identity.

### 24. Endpoint `GetInviteLinksByAccountancyEndpoint`

- **Camada**: Web
- **Tipo**: Endpoint
- **Acao**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/InviteLinks/GetInviteLinksByAccountancyEndpoint.cs`
- **Depende de**: #17, #22, #23
- **Spec-ref**: §3 "Admin consulta os links"
- **Detalhes**: `[HttpGet(IdentityRoutes.GetInviteLinks)]`, `[Authorize(Roles = "AccountingAdmin")]`. Request DTO com `[FromRoute] Guid AccountancyId`. Pipeline railway padrao. Retorna `InviteLinksResponse`.

### 25. Endpoint `RegenerateInviteLinkEndpoint`

- **Camada**: Web
- **Tipo**: Endpoint
- **Acao**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/InviteLinks/RegenerateInviteLinkEndpoint.cs`
- **Depende de**: #16, #22, #23
- **Spec-ref**: §3 "Rotacao"
- **Detalhes**: `[HttpPost(IdentityRoutes.RegenerateInviteLink)]`, `[Authorize(Roles = "AccountingAdmin")]`. Request com `[FromRoute] Guid AccountancyId` + `[FromRoute] string Role`. Retorna `InviteLinkResponse`.

### 26. Endpoint `GetInviteLinkInfoEndpoint`

- **Camada**: Web
- **Tipo**: Endpoint
- **Acao**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/InviteLinks/GetInviteLinkInfoEndpoint.cs`
- **Depende de**: #18, #22, #23
- **Spec-ref**: §4 "Info publica"
- **Detalhes**: `[HttpGet(IdentityRoutes.GetInviteLinkInfo)]`, `[AllowAnonymous]`. Request com `[FromQuery] string Token`. Retorna `InviteLinkInfoResponse`. 404 sem vazar se token invalido/regenerated.

### 27. Endpoint `RegisterByInviteLinkEndpoint`

- **Camada**: Web
- **Tipo**: Endpoint
- **Acao**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/InviteLinks/RegisterByInviteLinkEndpoint.cs`
- **Depende de**: #19, #22, #23
- **Spec-ref**: §4 "Registro publico"
- **Detalhes**: `[HttpPost(IdentityRoutes.RegisterByInviteLink)]`, `[AllowAnonymous]`. Request `[FromBody] RegisterByInviteLinkRequest`. Retorna `IdentifierResponse` (200 ou 201).

### 28. Consumer `ProjectionAccountancyInviteLinkConsumer`

- **Camada**: Web
- **Tipo**: ConsumerRegistration
- **Acao**: criar
- **Caminho**: `src/Web/ServiceInstallers/EventBus/Consumers/IdentityModule/ProjectionAccountancyInviteLinkConsumer.cs`
- **Depende de**: #2, #20
- **Spec-ref**: §3, §4 (read-side)
- **Detalhes**: Classe implementa `IConsumer<InviteLinkGenerated>`, `IConsumer<InviteLinkRegenerated>`, `IConsumer<InviteLinkUsed>`. Cada `Consume` delega ao `IProjectionAccountancyInviteLinkEventHandler`.

### 29. `ConsumerConfiguration` — Identity — tres novos endpoints

- **Camada**: Web
- **Tipo**: ConsumerRegistration
- **Acao**: modificar
- **Caminho**: `src/Web/ServiceInstallers/EventBus/Consumers/IdentityModule/ConsumerConfiguration.cs`
- **Depende de**: #28
- **Spec-ref**: obrigatorio por pattern
- **Detalhes**: Adicionar tres `bus.ConfigureEventReceiveEndpoint`:
  - `<ProjectionAccountancyInviteLinkConsumer, DomainEvents.InviteLinkGenerated>`
  - `<ProjectionAccountancyInviteLinkConsumer, DomainEvents.InviteLinkRegenerated>`
  - `<ProjectionAccountancyInviteLinkConsumer, DomainEvents.InviteLinkUsed>`

### 30. `InviteUserCommand` — trocar response para `UserInviteCreatedResponse`

- **Camada**: Shared
- **Tipo**: Command
- **Acao**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Commands/InviteUserCommand.cs`
- **Depende de**: #8
- **Spec-ref**: §2 "Email-based invite restrito a SystemAdmin"
- **Detalhes**: Trocar `ICommand<IdentifierResponse>` por `ICommand<UserInviteCreatedResponse>`. Assinatura do record fica `InviteUserCommand(string Email, string Role, Guid? AccountancyId)` — nao muda.

### 31. `InviteUserHandler` — retornar InviteLink na resposta

- **Camada**: Application
- **Tipo**: Handler
- **Acao**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Commands/InviteUserHandler.cs`
- **Depende de**: #30
- **Spec-ref**: §2 "SystemAdmin copia o link do response"
- **Detalhes**: Monta `inviteLink = $"{frontendOptions.Value.BaseUrl}/accept-invite/{rawToken}"` (ja existia). Passa a retornar `new UserInviteCreatedResponse(invite.Id, inviteLink)`. Email ainda e enfileirado via Hangfire — o link no response e complemento, nao substitui.

### 32. `InviteUserEndpoint` — restrito a SystemAdmin + trocar response type

- **Camada**: Web
- **Tipo**: Endpoint
- **Acao**: modificar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/InviteUserEndpoint.cs`
- **Depende de**: #30, #31
- **Spec-ref**: §2 "Restrito a SystemAdmin"
- **Detalhes**: Trocar `EndpointBaseAsync.WithRequest<InviteUserRequest>.WithActionResult<IdentifierResponse>` por `.WithActionResult<UserInviteCreatedResponse>`. Adicionar `[Authorize(Roles = "SystemAdmin")]`. Atualizar `[ProducesResponseType<UserInviteCreatedResponse>(200)]`.

### 33. Scaffold modulo Notifications — 5 projetos .csproj

- **Camada**: todas
- **Tipo**: ModuleScaffold
- **Acao**: criar
- **Caminho**: `src/Modules/Notifications/` (5 projetos)
- **Depende de**: nenhum
- **Spec-ref**: §5 "Novo modulo Notifications"
- **Detalhes**: Estrutura espelhando Identity:
  - `Notifications.Domain` — ref: Core.Domain
  - `Notifications.Shared` — ref: Core.Shared
  - `Notifications.Application` — ref: Domain, Shared, Core.Application, **Identity.Domain** (para tipos `ProjectionModel.User`) e **Identity.Persistence** (para `IIdentityProjection<>`)
  - `Notifications.Persistence` — ref: Domain, Core.Persistence
  - `Notifications.Infrastructure` — ref: Application, Persistence, Core.Infrastructure, **Identity.Shared** (para `DomainEvents.InviteLinkUsed` no consumer cross-module)
  - Cada projeto inclui `AssemblyReference.cs`
  - Adicionar todos ao `Societiza.sln`

### 34. SmartEnum `NotificationType`

- **Camada**: Domain
- **Tipo**: SmartEnum
- **Acao**: criar
- **Caminho**: `src/Modules/Notifications/Notifications.Domain/Enumerations/NotificationType.cs`
- **Depende de**: #33
- **Spec-ref**: §5 "Tipos iniciais"
- **Detalhes**: `InviteLinkUsed(0)` — MVP cobre so esse tipo. Aberto para crescer sem quebrar events (string no evento).

### 35. Domain Events do aggregate `Notification`

- **Camada**: Domain
- **Tipo**: DomainEvent
- **Acao**: criar
- **Caminho**: `src/Modules/Notifications/Notifications.Domain/Events/DomainEvents.cs`
- **Depende de**: #33, #34
- **Spec-ref**: §5 "Ciclo de vida da notificacao"
- **Detalhes**: Dois records:
  - `NotificationCreated(Guid Id, Guid RecipientUserId, string Type, string Title, string Body, Guid? RelatedEntityId, DateTimeOffset CreatedAt, ulong Version)`
  - `NotificationRead(Guid Id, DateTimeOffset ReadAt, ulong Version)`

### 36. Aggregate `Notification`

- **Camada**: Domain
- **Tipo**: Aggregate
- **Acao**: criar
- **Caminho**: `src/Modules/Notifications/Notifications.Domain/Aggregates/Notification.cs`
- **Depende de**: #34, #35
- **Spec-ref**: §5 "Ciclo"
- **Detalhes**: Metodos:
  - `static Create(Guid recipientUserId, NotificationType type, string title, string body, Guid? relatedEntityId)` — Id novo, levanta `NotificationCreated`.
  - `MarkAsRead(DateTimeOffset readAt)` — idempotente se ja lido (sem evento). Levanta `NotificationRead`.
  - `When(...)` para cada evento.

### 37. Projection Model `Notification`

- **Camada**: Domain
- **Tipo**: ProjectionModel
- **Acao**: criar
- **Caminho**: `src/Modules/Notifications/Notifications.Domain/Projection/ProjectionModel.cs`
- **Depende de**: #33
- **Spec-ref**: §5 "Leitura via feed do usuario"
- **Detalhes**: `public static class ProjectionModel` com record `Notification(Guid Id, Guid RecipientUserId, string Type, string Title, string Body, Guid? RelatedEntityId, bool IsRead, DateTimeOffset CreatedAt, DateTimeOffset? ReadAt, bool IsDeleted) : IProjectionModel`.

### 38. EF configuration — StoreEvent + Snapshot

- **Camada**: Persistence
- **Tipo**: EFConfiguration
- **Acao**: criar
- **Caminho**: `src/Modules/Notifications/Notifications.Persistence/Configurations/StoreEventConfiguration.cs` + `SnapshotConfiguration.cs`
- **Depende de**: #36
- **Spec-ref**: §5 (infra aggregate novo)
- **Detalhes**: `NotificationStoreEventConfiguration : StoreEventConfiguration<Notification>` e `NotificationSnapshotConfiguration : SnapshotConfiguration<Notification>`.

### 39. `NotificationsDbContext` + schema

- **Camada**: Persistence
- **Tipo**: EFConfiguration
- **Acao**: criar
- **Caminho**: `src/Modules/Notifications/Notifications.Persistence/Context/NotificationsDbContext.cs` + `Constants/Schemas.cs`
- **Depende de**: #38
- **Spec-ref**: §5 (infra)
- **Detalhes**: DbContext espelhando `IdentityDbContext`. Schema `notifications`. Aplica configurations do assembly. Registro em `PersistenceServiceInstaller` com Npgsql e migration history table no schema `notifications`.

### 40. `INotificationsProjectionDbContext` + `NotificationsProjectionDbContext` + `INotificationsProjection<T>` + `NotificationsProjection<T>`

- **Camada**: Persistence
- **Tipo**: EFConfiguration
- **Acao**: criar
- **Caminho**: `src/Modules/Notifications/Notifications.Persistence/{Context,Projection}/*.cs`
- **Depende de**: #33
- **Spec-ref**: §5 (infra)
- **Detalhes**: Segue o pattern do modulo Identity (`IIdentityProjection<>` → `IdentityProjection<>`). Connection string lida de `Projections:Notifications`.

### 41. Migration — InitialCreate (Notifications)

- **Camada**: Persistence
- **Tipo**: Migration
- **Acao**: criar
- **Caminho**: `src/Modules/Notifications/Notifications.Persistence/Migrations/*_InitialCreate.cs`
- **Depende de**: #38, #39
- **Spec-ref**: §5 (infra)
- **Detalhes**: `dotnet ef migrations add InitialCreate --project src/Modules/Notifications/Notifications.Persistence --startup-project src/Web --context NotificationsDbContext`. Cria tabelas `NotificationStoreEvents` e `NotificationSnapshots` no schema `notifications`.

### 42. Service `INotificationApplicationService`

- **Camada**: Application
- **Tipo**: Service
- **Acao**: criar
- **Caminho**: `src/Modules/Notifications/Notifications.Application/Services/INotificationApplicationService.cs`
- **Depende de**: #39
- **Spec-ref**: §5 (infra)
- **Detalhes**: Marker `public interface INotificationApplicationService : IApplicationService<NotificationsDbContext>`. Segue pattern.

### 43. Abstracao `INotificationBroadcaster`

- **Camada**: Application
- **Tipo**: Service
- **Acao**: criar
- **Caminho**: `src/Modules/Notifications/Notifications.Application/Services/INotificationBroadcaster.cs`
- **Depende de**: #37
- **Spec-ref**: §5 "Push em tempo real"
- **Detalhes**: `Task PushToUser(Guid userId, ProjectionModel.Notification notification, CancellationToken ct)`. Implementacao concreta vive na Web (SignalR) — Application nao depende de SignalR.

### 44. Response DTO `NotificationResponse`

- **Camada**: Shared
- **Tipo**: ResponseDTO
- **Acao**: criar
- **Caminho**: `src/Modules/Notifications/Notifications.Shared/Response/NotificationResponse.cs`
- **Depende de**: nenhum
- **Spec-ref**: §5 "Feed do usuario"
- **Detalhes**: `sealed record NotificationResponse(Guid Id, string Type, string Title, string Body, Guid? RelatedEntityId, bool IsRead, DateTimeOffset CreatedAt, DateTimeOffset? ReadAt)`.

### 45. Command `MarkNotificationReadCommand` + `CreateInviteLinkUsedNotificationCommand`

- **Camada**: Shared
- **Tipo**: Command
- **Acao**: criar
- **Caminho**: `src/Modules/Notifications/Notifications.Shared/Commands/*.cs`
- **Depende de**: nenhum
- **Spec-ref**: §5 "Marcar lida"; §4 (disparo cross-module)
- **Detalhes**:
  - `MarkNotificationReadCommand(Guid NotificationId) : ICommand` — user logado, ownership checada no handler.
  - `CreateInviteLinkUsedNotificationCommand(Guid AccountancyId, Guid UsedByUserId, string UsedByEmail, string Role, DateTimeOffset UsedAt) : ICommand` — disparado pelo consumer cross-module (§4). Primitivos apenas (Hangfire-safe, mas aqui vai sincrono via MediatR — mesma forma).

### 46. Query `ListMyNotificationsQuery`

- **Camada**: Shared
- **Tipo**: Query
- **Acao**: criar
- **Caminho**: `src/Modules/Notifications/Notifications.Shared/Queries/ListMyNotificationsQuery.cs`
- **Depende de**: #44
- **Spec-ref**: §5 "Feed"
- **Detalhes**: `ListMyNotificationsQuery() : IQuery<IReadOnlyList<NotificationResponse>>`. Handler resolve usuario via `ICurrentUserService`.

### 47. Validator `MarkNotificationReadValidator`

- **Camada**: Application
- **Tipo**: Validator
- **Acao**: criar
- **Caminho**: `src/Modules/Notifications/Notifications.Application/UseCases/Validators/MarkNotificationReadValidator.cs`
- **Depende de**: #45
- **Spec-ref**: §5
- **Detalhes**: `NotificationId` nao vazio.

### 48. Handler `CreateInviteLinkUsedNotificationHandler`

- **Camada**: Application
- **Tipo**: Handler
- **Acao**: criar
- **Caminho**: `src/Modules/Notifications/Notifications.Application/UseCases/Commands/CreateInviteLinkUsedNotificationHandler.cs`
- **Depende de**: #36, #42, #45
- **Spec-ref**: §4 "Admin e notificado quando alguem entra pelo link"
- **Detalhes**: Injeta `IIdentityProjection<ProjectionModel.User>` (cross-module, pragmatismo MVP — TODO futuro: evento dedicado `MembershipChanged`). Lista admins da contabilidade:
  - `admins = users.Where(u => u.AccountancyId == cmd.AccountancyId && u.Role == "AccountingAdmin" && !u.IsDeleted)`.
  - Se novo usuario e AccountingAdmin → remove ele proprio da lista (nao notifica ele mesmo).
  - Para cada admin: `Notification.Create(recipientUserId: admin.Id, type: InviteLinkUsed, title: "Novo membro", body: $"{email} entrou via link como {role}", relatedEntityId: cmd.UsedByUserId)` + `AppendEventsAsync`.

### 49. Handler `MarkNotificationReadHandler`

- **Camada**: Application
- **Tipo**: Handler
- **Acao**: criar
- **Caminho**: `src/Modules/Notifications/Notifications.Application/UseCases/Commands/MarkNotificationReadHandler.cs`
- **Depende de**: #36, #42, #45
- **Spec-ref**: §5 "Marcar lida"
- **Detalhes**: `LoadAggregateAsync<Notification>`. Valida ownership: `notification.RecipientUserId == currentUser.UserId` senao `ForbiddenError`. Chama `MarkAsRead(now)`. Append.

### 50. Handler `ListMyNotificationsHandler`

- **Camada**: Application
- **Tipo**: Handler
- **Acao**: criar
- **Caminho**: `src/Modules/Notifications/Notifications.Application/UseCases/Queries/ListMyNotificationsHandler.cs`
- **Depende de**: #37, #46
- **Spec-ref**: §5 "Feed"
- **Detalhes**: Injeta `INotificationsProjection<ProjectionModel.Notification>` + `ICurrentUserService`. `ListAsync(x => x.RecipientUserId == userId && !x.IsDeleted)`. Ordena desc por `CreatedAt`. Mapeia para `NotificationResponse`.

### 51. ProjectionHandler `IProjectionNotificationEventHandler` + impl

- **Camada**: Application
- **Tipo**: ProjectionHandler
- **Acao**: criar
- **Caminho**: `src/Modules/Notifications/Notifications.Application/UseCases/Events/ProjectionNotificationEventHandler.cs`
- **Depende de**: #35, #37, #43
- **Spec-ref**: §5 "Projecao + push"
- **Detalhes**: Interface composta `IEventHandler<NotificationCreated>` + `IEventHandler<NotificationRead>`. Handlers:
  - `NotificationCreated` → `ReplaceInsertAsync(new(...IsRead: false, ReadAt: null))` **e** `await broadcaster.PushToUser(ev.RecipientUserId, projection, ct)`.
  - `NotificationRead` → `existing with { IsRead = true, ReadAt = ev.ReadAt }`. Push opcional (MVP: sem push no read).

### 52. Registro do projection handler em `AddEventInteractors`

- **Camada**: Application
- **Tipo**: ConsumerRegistration
- **Acao**: criar
- **Caminho**: `src/Modules/Notifications/Notifications.Application/DependencyInjection/ServiceCollectionExtensions.cs`
- **Depende de**: #51
- **Spec-ref**: obrigatorio por pattern
- **Detalhes**: Metodo `AddEventInteractors(this IServiceCollection services)` com `.AddScoped<IProjectionNotificationEventHandler, ProjectionNotificationEventHandler>()`. Chamado em `ApplicationServiceInstaller.Install()`.

### 53. Service installers do modulo Notifications

- **Camada**: Infrastructure
- **Tipo**: Service
- **Acao**: criar
- **Caminho**: `src/Modules/Notifications/Notifications.Infrastructure/NotificationsModuleInstaller.cs` + `Notifications.{Application,Infrastructure,Persistence}/ServiceInstallers/*.cs`
- **Depende de**: #40, #42, #52
- **Spec-ref**: obrigatorio por pattern
- **Detalhes**: Espelho do padrao do modulo Identity. `ModuleInstaller` agrega os tres `IServiceInstaller`: Application (MediatR, validators, AddEventInteractors), Infrastructure (Scrutor), Persistence (DbContext + open generics `INotificationsProjection<>` → `NotificationsProjection<>`).

### 54. Hub `NotificationsHub`

- **Camada**: Web
- **Tipo**: Service
- **Acao**: criar
- **Caminho**: `src/Web/Hubs/NotificationsHub.cs`
- **Depende de**: nenhum
- **Spec-ref**: §5 "SignalR"
- **Detalhes**: `[Authorize] public class NotificationsHub : Hub`. Em `OnConnectedAsync`: resolve `userId` via `Context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value`. Adiciona ao grupo `user:{userId}`. Em `OnDisconnectedAsync`: remove do grupo.

### 55. Impl `SignalRNotificationBroadcaster`

- **Camada**: Web
- **Tipo**: Service
- **Acao**: criar
- **Caminho**: `src/Web/Services/SignalRNotificationBroadcaster.cs`
- **Depende de**: #43, #54
- **Spec-ref**: §5
- **Detalhes**: `public class SignalRNotificationBroadcaster(IHubContext<NotificationsHub> hub) : INotificationBroadcaster`. Metodo: `hub.Clients.Group($"user:{userId}").SendAsync("notification", mappedPayload, ct)`.

### 56. `SignalRServiceInstaller`

- **Camada**: Web
- **Tipo**: Service
- **Acao**: criar
- **Caminho**: `src/Web/ServiceInstallers/SignalR/SignalRServiceInstaller.cs`
- **Depende de**: #54, #55
- **Spec-ref**: §5
- **Detalhes**: `services.AddSignalR()`. Registra `INotificationBroadcaster → SignalRNotificationBroadcaster` como singleton (IHubContext e singleton-friendly).

### 57. JWT Bearer — aceitar `access_token` em querystring nas rotas `/hubs/*`

- **Camada**: Web
- **Tipo**: Service
- **Acao**: modificar
- **Caminho**: `src/Web/ServiceInstallers/Authentication/AuthenticationServiceInstaller.cs` (ou onde o JwtBearerOptions e configurado)
- **Depende de**: nenhum
- **Spec-ref**: §5 "SignalR autenticado"
- **Detalhes**: No `JwtBearerOptions.Events.OnMessageReceived`: se `context.Request.Path.StartsWithSegments("/hubs")` e ha `access_token` na querystring, copia para `context.Token`. Padrao Microsoft para SignalR + JWT.

### 58. Rotas `NotificationsRoutes`

- **Camada**: Web
- **Tipo**: Endpoint (routes)
- **Acao**: criar
- **Caminho**: `src/Web/Endpoints/Routes/NotificationsRoutes.cs`
- **Depende de**: nenhum
- **Spec-ref**: §5
- **Detalhes**: `BaseUri = "v{version:apiVersion}/notifications"`. `NotificationId = "notificationId"`. `ListMine = $"{BaseUri}"`. `MarkRead = $"{BaseUri}/{{{NotificationId}:guid}}/read"`.

### 59. Tag `Tags.Notifications`

- **Camada**: Web
- **Tipo**: Endpoint (tags)
- **Acao**: modificar
- **Caminho**: `src/Web/Endpoints/Tags.cs`
- **Depende de**: nenhum
- **Spec-ref**: Swagger
- **Detalhes**: Adicionar `Notifications = "Notifications"`.

### 60. Endpoint `ListMyNotificationsEndpoint`

- **Camada**: Web
- **Tipo**: Endpoint
- **Acao**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Notifications/ListMyNotificationsEndpoint.cs`
- **Depende de**: #50, #58, #59
- **Spec-ref**: §5 "Feed"
- **Detalhes**: `[HttpGet(NotificationsRoutes.ListMine)]`, `[Authorize]`. Retorna `IReadOnlyList<NotificationResponse>`.

### 61. Endpoint `MarkNotificationReadEndpoint`

- **Camada**: Web
- **Tipo**: Endpoint
- **Acao**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Notifications/MarkNotificationReadEndpoint.cs`
- **Depende de**: #49, #58, #59
- **Spec-ref**: §5
- **Detalhes**: `[HttpPost(NotificationsRoutes.MarkRead)]`, `[Authorize]`. Request com `[FromRoute] Guid NotificationId`.

### 62. Consumer `ProjectionNotificationConsumer`

- **Camada**: Web
- **Tipo**: ConsumerRegistration
- **Acao**: criar
- **Caminho**: `src/Web/ServiceInstallers/EventBus/Consumers/NotificationsModule/ProjectionNotificationConsumer.cs`
- **Depende de**: #35, #51
- **Spec-ref**: §5
- **Detalhes**: `IConsumer<NotificationCreated>`, `IConsumer<NotificationRead>`. Delega ao handler.

### 63. Consumer cross-module `InviteLinkUsedNotificationConsumer`

- **Camada**: Web
- **Tipo**: ConsumerRegistration
- **Acao**: criar
- **Caminho**: `src/Web/ServiceInstallers/EventBus/Consumers/NotificationsModule/InviteLinkUsedNotificationConsumer.cs`
- **Depende de**: #2 (Identity event), #48
- **Spec-ref**: §4 (disparo cross-module)
- **Detalhes**: `IConsumer<DomainEvents.InviteLinkUsed>` (tipo do Identity). Em `Consume`, dispara via `ISender`: `new CreateInviteLinkUsedNotificationCommand(ev.AccountancyId, ev.UsedByUserId, ev.UsedByEmail, ev.Role, ev.UsedAt)`. **Boundary explicita**: consumer registrado no modulo Notifications, consumindo evento do Identity — integracao canonica via event bus, sem acoplamento direto entre Application layers.

### 64. `ConsumerConfiguration` — Notifications

- **Camada**: Web
- **Tipo**: ConsumerRegistration
- **Acao**: criar
- **Caminho**: `src/Web/ServiceInstallers/EventBus/Consumers/NotificationsModule/ConsumerConfiguration.cs`
- **Depende de**: #62, #63
- **Spec-ref**: obrigatorio por pattern
- **Detalhes**: `IEventReceiveEndpointConfiguration`. Registra tres endpoints com `MODULE_NAME = "notifications"`:
  - `<ProjectionNotificationConsumer, NotificationCreated>`
  - `<ProjectionNotificationConsumer, NotificationRead>`
  - `<InviteLinkUsedNotificationConsumer, DomainEvents.InviteLinkUsed>` (Identity namespace)

### 65. `Program.cs` — registrar modulo Notifications + MapHub + MigrateDatabase

- **Camada**: Web
- **Tipo**: Service
- **Acao**: modificar
- **Caminho**: `src/Web/Program.cs` + `src/Web/StartupTasks/MigrateDatabaseStartupTask.cs` + `src/Web/Web.csproj` + `src/Web/appsettings*.json`
- **Depende de**: #39, #41, #53, #54, #56
- **Spec-ref**: §5 (bootstrap)
- **Detalhes**:
  - `Web.csproj`: adicionar `ProjectReference` para os 5 projetos Notifications.
  - `Program.cs`: adicionar `Notifications.Infrastructure.AssemblyReference.Assembly` na chamada `InstallServicesFromAssemblies(...)` de service installers e MassTransit consumers.
  - `Program.cs`: apos `app.UseAuthorization()`, `app.MapHub<NotificationsHub>("/hubs/notifications");`.
  - `MigrateDatabaseStartupTask`: adicionar `await MigrateDatabaseAsync<NotificationsDbContext>(scope, stoppingToken)`.
  - `appsettings.Development.json` + `.env.example`: adicionar `ConnectionStrings:Notifications` (Postgres) e `Projections:Notifications` (Mongo).

---

## Checklist de registro

- [x] **`ConsumerConfiguration.cs` (Identity)** — item #29. Adicionar tres `ConfigureEventReceiveEndpoint` para `InviteLinkGenerated`, `InviteLinkRegenerated`, `InviteLinkUsed` via `ProjectionAccountancyInviteLinkConsumer`.
- [x] **`ConsumerConfiguration.cs` (Notifications)** — item #64. Novo arquivo. Tres endpoints: dois para eventos proprios (`NotificationCreated`, `NotificationRead`) via `ProjectionNotificationConsumer` + um cross-module (`Identity.DomainEvents.InviteLinkUsed`) via `InviteLinkUsedNotificationConsumer`.
- [x] **`AddEventInteractors()` (Identity)** — item #21. Adicionar `IProjectionAccountancyInviteLinkEventHandler → ProjectionAccountancyInviteLinkEventHandler`.
- [x] **`AddEventInteractors()` (Notifications)** — item #52. Novo metodo no novo `ServiceCollectionExtensions` do modulo. Registra `IProjectionNotificationEventHandler → ProjectionNotificationEventHandler`.
- [x] **`MigrateDatabaseStartupTask`** — item #65. Adicionar `NotificationsDbContext`.
- [ ] **`RegisterRecurringJobsStartupTask`** — nao tocar. Nenhum job recorrente novo nesta feature.
- [x] **`PersistenceServiceInstaller` (Notifications)** — item #53. Registro explicito de `INotificationsProjection<>` open-generic (Scrutor nao cobre open generics) + `NotificationsDbContext` + `INotificationApplicationService → ApplicationService<NotificationsDbContext>`.
- [x] **`PersistenceServiceInstaller` (Identity)** — nao tocar. `IIdentityProjection<>` ja esta registrado — a projection `AccountancyInviteLink` usa o generic existente.
- [x] **`InfrastructureServiceInstaller` (Identity)** — item #13. Scrutor pega `InviteLinkTokenService` automaticamente (implementa `IInviteLinkTokenService`).
- [x] **`AuthenticationServiceInstaller`** — item #57. Adicionar `OnMessageReceived` para ler `access_token` da querystring em rotas `/hubs/*`.
- [x] **`SignalRServiceInstaller`** — item #56. Novo installer. `AddSignalR()` + `INotificationBroadcaster → SignalRNotificationBroadcaster`.
- [x] **`Program.cs`** — item #65. `MapHub<NotificationsHub>("/hubs/notifications")` apos `UseAuthorization`. `AssemblyReference.Assembly` de Notifications no install de modulos.
- [x] **`Web.csproj`** — item #65. `ProjectReference` para os 5 projetos Notifications.
- [x] **`Societiza.sln`** — item #33. Adicionar os 5 projetos.
- [x] **`appsettings*.json` + `.env.example`** — item #65. `ConnectionStrings:Notifications` + `Projections:Notifications`.

---

## Riscos e pontos de atencao

- **GUIDv5 deterministico (item #3)** — pattern novo no repo. Garante uniqueness de stream `(AccountancyId, Role)` no event store sem lookup previo. Comentar no codigo com uma linha curta explicando a invariante. Namespace fixo guardado como `const Guid` dentro do aggregate.
- **Raw token persistido na projecao (item #4)** — decisao de produto. Link e compartilhado por WhatsApp, o admin precisa copiar de novo apos refresh da tela. Ameaca de forca bruta e mitigada por 256 bits de entropia; ameaca de leitor do Mongo e aceita no MVP. XML doc do campo deve declarar isso. Nao vazar o token em logs (sanitizar).
- **Cross-module read: Notifications → `IIdentityProjection<User>` (item #48)** — pragmatismo MVP. Alternativa canonica seria publicar um evento `MembershipChanged` no Identity e manter replica local no Notifications. Deixar `TODO` no handler documentando a evolucao. Risco aceitavel porque Notifications so faz leitura, Identity continua sendo o dono da escrita.
- **Consumer cross-module `InviteLinkUsedNotificationConsumer` (item #63)** — vive em `Web/ServiceInstallers/EventBus/Consumers/NotificationsModule/` mas referencia `Identity.Shared.Events.DomainEvents.InviteLinkUsed`. Essa e a forma canonica do repo (Web ja consome events do proprio Identity). Queue name gerado: `webbff.notifications.invite-link-used-notification-consumer.invite-link-used`.
- **SignalR + JWT via querystring (item #57)** — padrao Microsoft para `HttpClient` nao suportar headers custom no handshake websocket. Restrito a rotas `/hubs/*` — nao afeta seguranca dos endpoints REST. Frontend passa `?access_token=...` so na conexao do hub.
- **Convention tests** — `tests/Web.Tests` valida Swagger em portugues + `OperationId` + `Tags`. Todos os 6 novos endpoints (#24, #25, #26, #27, #60, #61) + endpoint modificado (#32) precisam atender.
- **Breaking change em `InviteUserCommand` (item #30)** — o response type muda de `IdentifierResponse` para `UserInviteCreatedResponse`. Frontend que consome `POST /v1/identity/invite` precisa atualizar. Como a rota passa a exigir `SystemAdmin`, o consumidor provavel e apenas o painel interno — confirmar com o humano antes do merge.
- **Reuso do slot `inviteId` em `User.Create` (item #19)** — passamos `link.Id` no parametro `inviteId` do aggregate `User`. Manter o nome atual do parametro para nao criar breaking change no aggregate, mas o slot agora significa "convite por email **ou** por link". Comentar isso em uma linha no XML doc de `User.Create`.
- **Remocao de membros nao entra no MVP** — pre-condicao #4. Se o humano quiser incluir apos revisao da spec, entra como plano separado.
- **Multi-membership deferido** — pre-condicao #1. Email duplicado em contabilidade diferente retorna 409 no MVP. Se evoluir, exigira migrar `User.AccountancyId` escalar para coleção, alterar JWT claims, alterar queries de listagem. Nao entra neste plano.
- **Impacto de restringir `InviteUserEndpoint` a SystemAdmin (item #32)** — hoje o endpoint e usado pelo AccountingAdmin para convidar membros? Verificar com o humano. Se sim, o convite via email passa a ser responsabilidade exclusiva da ferramenta interna; AccountingAdmin agora usa apenas o link generico.
- **SignalR sem backplane** — MVP assume instancia unica do Web. Se houver scale-out horizontal, e preciso Redis backplane para distribuir `Clients.Group(...)`. Nao entra neste plano — documentar como item futuro.
