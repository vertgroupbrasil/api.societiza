# Plano — identity-invite-link

**Spec**: specs/identity-invite-link/spec.md @ product.societiza
**Módulos afetados**: Identity, Web
**Módulo novo?**: não
**Resumo**: Adiciona aggregate `AccountancyInviteLink` com link genérico de equipe (7 dias) para o cenário AccountingAdmin → AccountingEmployee. Modifica o fluxo de convite do SystemAdmin para retornar o link gerado na resposta, além do e-mail já existente. Não altera o aggregate `User` (decisão de produto: um usuário pertence a exatamente uma contabilidade).

---

## Pré-condições

Nenhuma.

---

## Itens

### 1. SmartEnum `InviteLinkStatus`

- **Camada**: Domain
- **Tipo**: SmartEnum
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Domain/Enumerations/InviteLinkStatus.cs`
- **Depende de**: nenhum
- **Spec-ref**: §9 "validade 7 dias", "renovação manual", "link para de aceitar cadastros"
- **Detalhes**: `Active(0)`, `Expired(1)`, `Regenerated(2)`. `Expired` setado pelo job horário. `Regenerated` marca versões anteriores após rotação manual — mantidas historicamente no event stream, mas não aceitam novos registros.

---

### 2. Domain events de `AccountancyInviteLink`

- **Camada**: Domain
- **Tipo**: DomainEvent
- **Ação**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Domain/Events/DomainEvents.cs`
- **Depende de**: #1
- **Spec-ref**: §5 "link sempre visível", §9 "renovação manual", "N pessoas entraram"
- **Detalhes**: Adicionar quatro records na region `AccountancyInviteLink Events`:
  - `InviteLinkGenerated(Guid Id, Guid AccountancyId, string Token, string TokenHash, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, ulong Version)` — `Token` raw persiste na projeção para o botão "Copiar".
  - `InviteLinkRegenerated(Guid Id, string Token, string TokenHash, DateTimeOffset RegeneratedAt, DateTimeOffset ExpiresAt, ulong Version)` — reseta a janela de 7 dias.
  - `InviteLinkUsed(Guid Id, Guid AccountancyId, Guid UsedByUserId, string UsedByEmail, int UsageCount, DateTimeOffset UsedAt, ulong Version)` — incrementa contador; `UsageCount` é o novo total após o registro.
  - `InviteLinkExpired(Guid Id, DateTimeOffset ExpiredAt, ulong Version)` — disparado pelo job recorrente.

---

### 3. Aggregate `AccountancyInviteLink`

- **Camada**: Domain
- **Tipo**: Aggregate
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Domain/Aggregates/AccountancyInviteLink.cs`
- **Depende de**: #1, #2
- **Spec-ref**: §4 cenário 3, §5 "exatamente um link ativo por contabilidade", §7 "um link de equipe ativo"
- **Detalhes**:
  - **Id determinístico**: GUIDv5 calculado sobre `AccountancyId` com namespace fixo `private static readonly Guid NAMESPACE = Guid.Parse("...")`. Garante exatamente um stream por contabilidade sem lookup prévio — tentativa de `Create` duplicada falha no event store por conflito de versão. Expor `static Guid GetId(Guid accountancyId)` para uso nos handlers.
  - Propriedades: `Guid AccountancyId`, `string RawToken` (apenas em memória — para montar response sem ler a projeção), `string TokenHash`, `InviteLinkStatus Status`, `int UsageCount`, `DateTimeOffset ExpiresAt`.
  - `static Create(Guid accountancyId, string rawToken, string tokenHash, DateTimeOffset now)` → raise `InviteLinkGenerated`, `ExpiresAt = now.AddDays(7)`.
  - `Regenerate(string newRawToken, string newTokenHash, DateTimeOffset now)` — apenas se `Status is Active or Expired`; caso contrário, invariante violada (não raise). Raise `InviteLinkRegenerated`.
  - `RegisterUsage(Guid userId, string email, DateTimeOffset usedAt)` — apenas se `Status == Active && ExpiresAt > usedAt`; invariante violada caso contrário. Incrementa `UsageCount` no evento.
  - `Expire(DateTimeOffset expiredAt)` — idempotente; raise `InviteLinkExpired` apenas se `Status == Active`.

---

### 4. Projection model `AccountancyInviteLink`

- **Camada**: Domain
- **Tipo**: ProjectionModel
- **Ação**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Domain/Projection/ProjectionModel.cs`
- **Depende de**: #1
- **Spec-ref**: §5 "gerado em · expira em · N pessoas entraram · botão Copiar"
- **Detalhes**: Adicionar record ao `static class ProjectionModel`:
  ```
  record AccountancyInviteLink(
      Guid Id, Guid AccountancyId,
      string Token,        // raw — necessário para botão "Copiar" após F5
      string TokenHash,
      int UsageCount,
      string Status,
      DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt,
      DateTimeOffset? LastUsedAt, DateTimeOffset? UpdatedAt,
      bool IsDeleted = false
  ) : IProjectionModel
  ```

---

### 5. EF `StoreEventConfiguration` para `AccountancyInviteLink`

- **Camada**: Persistence
- **Tipo**: EFConfiguration
- **Ação**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Persistence/Configurations/StoreEventConfiguration.cs`
- **Depende de**: #3
- **Spec-ref**: infra obrigatória por aggregate novo
- **Detalhes**: Adicionar classe vazia `AccountancyInviteLinkStoreEventConfiguration : StoreEventConfiguration<AccountancyInviteLink>` dentro da classe wrapper `StoreEventConfiguration`.

---

### 6. EF `SnapshotConfiguration` para `AccountancyInviteLink`

- **Camada**: Persistence
- **Tipo**: EFConfiguration
- **Ação**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Persistence/Configurations/SnapshotConfiguration.cs`
- **Depende de**: #3
- **Spec-ref**: infra obrigatória por aggregate novo
- **Detalhes**: Adicionar `AccountancyInviteLinkSnapshotConfiguration : SnapshotConfiguration<AccountancyInviteLink>` dentro da classe wrapper `SnapshotConfiguration`.

---

### 7. Migration `AddAccountancyInviteLinkAggregate`

- **Camada**: Persistence
- **Tipo**: Migration
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Persistence/Migrations/`
- **Depende de**: #5, #6
- **Spec-ref**: infra
- **Detalhes**: `dotnet ef migrations add AddAccountancyInviteLinkAggregate --project src/Modules/Identity/Identity.Persistence --startup-project src/Web --context IdentityDbContext`. Cria as tabelas `AccountancyInviteLinkStoreEvents` e `AccountancyInviteLinkSnapshots` no schema `identity`.

---

### 8. Service `IInviteLinkTokenService` + implementação

- **Camada**: Application (interface) + Infrastructure (impl)
- **Tipo**: Handler (service)
- **Ação**: criar
- **Caminho**:
  - `src/Modules/Identity/Identity.Application/Services/Abstractions/IInviteLinkTokenService.cs`
  - `src/Modules/Identity/Identity.Infrastructure/Services/Abstractions/InviteLinkTokenService.cs`
- **Depende de**: nenhum
- **Spec-ref**: §9 (tokens de 7 dias com entropia adequada)
- **Detalhes**: Interface com dois métodos: `string GenerateRawToken()` (32 bytes `RandomNumberGenerator` → base64url sem padding) e `string HashToken(string rawToken)` (SHA-256 → base64). A implementação é auto-descoberta pelo Scrutor via `ITransient`. Evita duplicar código de crypto em 3+ handlers.

---

### 9. Response DTOs do link de equipe

- **Camada**: Shared
- **Tipo**: ResponseDTO
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Response/InviteLinkResponse.cs`
- **Depende de**: nenhum
- **Spec-ref**: §5 "gerado em · expira em · N pessoas", §4 cenários 1 e 2 "link aparece na tela"
- **Detalhes**: Três records no mesmo arquivo:
  - `InviteLinkResponse(Guid Id, Guid AccountancyId, string Url, int UsageCount, DateTimeOffset? LastUsedAt, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, string Status)` — `Url = {FrontendBaseUrl}/join/{token}`.
  - `InviteLinkInfoResponse(Guid AccountancyId, string AccountancyName)` — exibido na página pública de registro (spec §5 "exibe o nome da contabilidade").
  - `UserInviteCreatedResponse(Guid Id, string InviteLink)` — substitui `IdentifierResponse` no fluxo do SystemAdmin (spec §5 "link aparece na tela após criação").

---

### 10. Modificar `InviteUserCommand` — trocar tipo de response

- **Camada**: Shared
- **Tipo**: Command
- **Ação**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Commands/InviteUserCommand.cs`
- **Depende de**: #9
- **Spec-ref**: §4 cenários 1 e 2, §5 "link aparece na tela após criação"
- **Detalhes**: `ICommand<IdentifierResponse>` → `ICommand<UserInviteCreatedResponse>`. Assinatura (`Email, Role, AccountancyId`) inalterada.

---

### 11. Command `RegenerateInviteLinkCommand`

- **Camada**: Shared
- **Tipo**: Command
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Commands/RegenerateInviteLinkCommand.cs`
- **Depende de**: #9
- **Spec-ref**: §9 "renovação manual pelo AccountingAdmin"
- **Detalhes**: `sealed record RegenerateInviteLinkCommand(Guid AccountancyId) : ICommand<InviteLinkResponse>`.

---

### 12. Command `RegisterByInviteLinkCommand`

- **Camada**: Shared
- **Tipo**: Command
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Commands/RegisterByInviteLinkCommand.cs`
- **Depende de**: nenhum
- **Spec-ref**: §4 cenário 3, §9 "usuário preenche nome, email e senha"
- **Detalhes**: `sealed record RegisterByInviteLinkCommand(string Token, string FirstName, string LastName, string Email, string Password, string ConfirmPassword) : ICommand`.

---

### 13. Query `GetInviteLinkByAccountancyQuery`

- **Camada**: Shared
- **Tipo**: Query
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Queries/GetInviteLinkByAccountancyQuery.cs`
- **Depende de**: #9
- **Spec-ref**: §5 "link sempre visível na área de membros"
- **Detalhes**: `sealed record GetInviteLinkByAccountancyQuery(Guid AccountancyId) : IQuery<InviteLinkResponse>`. Handler usa lazy-create: se não existe stream, cria o aggregate na hora da primeira consulta.

---

### 14. Query `GetInviteLinkInfoQuery`

- **Camada**: Shared
- **Tipo**: Query
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Queries/GetInviteLinkInfoQuery.cs`
- **Depende de**: #9
- **Spec-ref**: §5 "página de registro exibe nome da contabilidade"
- **Detalhes**: `sealed record GetInviteLinkInfoQuery(string Token) : IQuery<InviteLinkInfoResponse>`. Público (sem autenticação); hasheia o token para localizar a projeção.

---

### 15. Validator `RegenerateInviteLinkValidator`

- **Camada**: Application
- **Tipo**: Validator
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Validators/RegenerateInviteLinkValidator.cs`
- **Depende de**: #11
- **Spec-ref**: §9
- **Detalhes**: `AccountancyId` não vazio e diferente de `Guid.Empty`.

---

### 16. Validator `RegisterByInviteLinkValidator`

- **Camada**: Application
- **Tipo**: Validator
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Validators/RegisterByInviteLinkValidator.cs`
- **Depende de**: #12
- **Spec-ref**: §9 "preenche nome, email e senha"
- **Detalhes**: Token não vazio; `FirstName`/`LastName` não vazios (≤ 100 chars); e-mail válido (≤ 150 chars); `Password` atende política existente (reutilizar as regras de `AcceptInviteValidator`); `ConfirmPassword == Password`.

---

### 17. Handler `GetInviteLinkByAccountancyHandler`

- **Camada**: Application
- **Tipo**: Handler
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Queries/GetInviteLinkByAccountancyHandler.cs`
- **Depende de**: #3, #4, #8, #9, #13
- **Spec-ref**: §5 "link sempre visível com botão Copiar", §9 "lazy-create"
- **Detalhes**: Resolve `id = AccountancyInviteLink.GetId(query.AccountancyId)`. `LoadAggregateAsync<AccountancyInviteLink>(id)` — se `NotFound`: gera token via `IInviteLinkTokenService`, chama `AccountancyInviteLink.Create(...)`, `AppendEventsAsync`; monta response com `Url = {FrontendOptions.BaseUrl}/join/{rawToken}` a partir do aggregate em memória. Se link existe e `Status != Active` ou `ExpiresAt <= now`: retorna response com `Status` real (frontend exibe "Expirado — Renovar"). Para buscar a URL do token quando o link já existe: usa `RawToken` em memória (disponível após `LoadAggregate`).

---

### 18. Handler `RegenerateInviteLinkHandler`

- **Camada**: Application
- **Tipo**: Handler
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Commands/RegenerateInviteLinkHandler.cs`
- **Depende de**: #3, #4, #8, #9, #11, #15
- **Spec-ref**: §9 "renovação manual", "reseta janela de 7 dias"
- **Detalhes**: Resolve id determinístico; `LoadAggregateAsync` — se `NotFound` comporta como lazy-create (mesmo fluxo de #17). Se existe: gera novo token via `IInviteLinkTokenService`, chama `link.Regenerate(newToken, newHash, now)`. `AppendEventsAsync`. Retorna `InviteLinkResponse` com `Url` montada do `RawToken` do aggregate.

---

### 19. Handler `GetInviteLinkInfoHandler`

- **Camada**: Application
- **Tipo**: Handler
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Queries/GetInviteLinkInfoHandler.cs`
- **Depende de**: #4, #8, #9, #14
- **Spec-ref**: §5 "página de registro exibe nome da contabilidade"
- **Detalhes**: Hasheia o token via `IInviteLinkTokenService.HashToken`. Busca projeção `TokenHash == hash && Status == Active.Name && ExpiresAt > now`. Se não achar: `NotFoundError` genérico (não vaza se expirou vs. inexistente). Injeta `IAccountancyProjection<Accountancy.Domain.Projection.ProjectionModel.Accountancy>` para buscar o nome — leitura cross-module pontual e somente leitura. Retorna `InviteLinkInfoResponse(link.AccountancyId, accountancy.TradeName ?? accountancy.LegalName)`.

---

### 20. Handler `RegisterByInviteLinkHandler`

- **Camada**: Application
- **Tipo**: Handler
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Commands/RegisterByInviteLinkHandler.cs`
- **Depende de**: #3, #4, #8, #9, #12, #16
- **Spec-ref**: §4 cenário 3, §9 "usuário preenche nome, email e senha"
- **Detalhes**:
  1. Hasheia token; busca projeção `TokenHash == hash && Status == Active.Name && ExpiresAt > now`. Não achar → `NotFoundError("Link.NotFoundOrExpired")`.
  2. Busca `User` por e-mail na projeção. Existe → `ConflictError("User.AlreadyExists")` (decisão de produto: um usuário por contabilidade).
  3. Hash da senha via `IPasswordService`.
  4. `User.Create(link.Id, firstName, lastName, email, passwordHash, UserRole.AccountingEmployee, link.AccountancyId)`.
  5. `LoadAggregateAsync<AccountancyInviteLink>(link.Id)` → `link.RegisterUsage(user.Id, email, now)`.
  6. `AppendEventsAsync(user)` + `AppendEventsAsync(link)`.

---

### 21. Projection handler `ProjectionAccountancyInviteLinkEventHandler`

- **Camada**: Application
- **Tipo**: ProjectionHandler
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Events/ProjectionAccountancyInviteLinkEventHandler.cs`
- **Depende de**: #2, #4
- **Spec-ref**: read-side de todas as operações do link
- **Detalhes**: Interface composta `IProjectionAccountancyInviteLinkEventHandler` implementa `IEventHandler<>` para os 4 eventos. Padrão `try/catch` + rethrow `InvalidOperationException` em cada `Handle`. Comportamentos:
  - `InviteLinkGenerated` → `ReplaceInsertAsync` com novo documento (Status=Active, UsageCount=0).
  - `InviteLinkRegenerated` → `FindAsync` + `with { Token, TokenHash, Status=Active, ExpiresAt, UpdatedAt }`.
  - `InviteLinkUsed` → `FindAsync` + `with { UsageCount = @event.UsageCount, LastUsedAt = @event.UsedAt, UpdatedAt }`.
  - `InviteLinkExpired` → `FindAsync` + `with { Status=Expired, UpdatedAt }`.

---

### 22. Modificar `InviteUserHandler` — retornar link na resposta

- **Camada**: Application
- **Tipo**: Handler
- **Ação**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Commands/InviteUserHandler.cs`
- **Depende de**: #9, #10
- **Spec-ref**: §5 "link aparece na tela após criação", §4 cenários 1 e 2
- **Detalhes**: Handler já constrói `inviteLink` com `rawToken`. Alterar assinatura para `ICommandHandler<InviteUserCommand, UserInviteCreatedResponse>` e retornar `new UserInviteCreatedResponse(invite.Id, inviteLink)`. E-mail via Hangfire permanece inalterado.

---

### 23. Job `ExpireInviteLinksJob`

- **Camada**: Application
- **Tipo**: RecurringJob
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Jobs/ExpireInviteLinksJob.cs`
- **Depende de**: #3, #4
- **Spec-ref**: §9 "após 7 dias link para de aceitar cadastros"
- **Detalhes**: `internal sealed record ExpireInviteLinksCommand() : ICommand`. Handler injeta `IIdentityApplicationService` e `IIdentityProjection<ProjectionModel.AccountancyInviteLink>`. Busca `Status == Active.Name && ExpiresAt < now` via `ListAsync`. Para cada: `LoadAggregateAsync<AccountancyInviteLink>`, `link.Expire(now)`, `AppendEventsAsync`. Usa `Parallel.ForEachAsync` com `MaxDegreeOfParallelism = 5` (mesmo padrão de `ExpireInvitesJob`). Log warning em falha individual + continue.

---

### 24. Registro de `ProjectionAccountancyInviteLinkEventHandler` em `AddEventInteractors()`

- **Camada**: Application
- **Tipo**: ConsumerRegistration
- **Ação**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Application/DependencyInjection/ServiceCollectionExtensions.cs`
- **Depende de**: #21
- **Spec-ref**: obrigatório — projection handlers não são descobertos pelo Scrutor
- **Detalhes**: Encadear `.AddScoped<IProjectionAccountancyInviteLinkEventHandler, ProjectionAccountancyInviteLinkEventHandler>()`.

---

### 25. Consumer `ProjectionAccountancyInviteLinkConsumer`

- **Camada**: Web
- **Tipo**: ConsumerRegistration
- **Ação**: criar
- **Caminho**: `src/Web/ServiceInstallers/EventBus/Consumers/IdentityModule/ProjectionAccountancyInviteLinkConsumer.cs`
- **Depende de**: #2, #21
- **Spec-ref**: integração RabbitMQ → MongoDB obrigatória por evento
- **Detalhes**: Implementa `IConsumer<>` para os 4 eventos do link, delegando cada `Consume` ao `IProjectionAccountancyInviteLinkEventHandler` injetado no construtor. Zero lógica no consumer.

---

### 26. `ConsumerConfiguration` — 4 novos endpoints de link

- **Camada**: Web
- **Tipo**: ConsumerRegistration
- **Ação**: modificar
- **Caminho**: `src/Web/ServiceInstallers/EventBus/Consumers/IdentityModule/ConsumerConfiguration.cs`
- **Depende de**: #2, #25
- **Spec-ref**: obrigatório por evento novo
- **Detalhes**: Adicionar 4 chamadas `ConfigureEventReceiveEndpoint<ProjectionAccountancyInviteLinkConsumer, IdentityDomainEvent.{Event}>(registrationContext, MODULE_NAME)` — uma por evento (`InviteLinkGenerated`, `InviteLinkRegenerated`, `InviteLinkUsed`, `InviteLinkExpired`). Agrupar sob comentário `// AccountancyInviteLink events`.

---

### 27. Registro do job em `RegisterRecurringJobsStartupTask`

- **Camada**: Web
- **Tipo**: RecurringJob
- **Ação**: modificar
- **Caminho**: `src/Web/StartupTasks/RegisterRecurringJobsStartupTask.cs`
- **Depende de**: #23
- **Spec-ref**: §9 "após 7 dias"
- **Detalhes**: `jobScheduler.ScheduleRecurring("expire-invite-links", "0 * * * *", new ExpireInviteLinksCommand())`. Mesmo cron do job de convites por e-mail (horário).

---

### 28. Tags `Identity.InviteLinks`

- **Camada**: Web
- **Tipo**: Endpoint
- **Ação**: modificar
- **Caminho**: `src/Web/Endpoints/Tags.cs`
- **Depende de**: nenhum
- **Spec-ref**: Swagger — separação visual dos endpoints de link vs. convites por e-mail
- **Detalhes**: Adicionar `internal const string InviteLinks = "Identity / Invite Links";` dentro de `static class Identity`.

---

### 29. Rotas novas em `IdentityRoutes`

- **Camada**: Web
- **Tipo**: Endpoint
- **Ação**: modificar
- **Caminho**: `src/Web/Endpoints/Routes/IdentityRoutes.cs`
- **Depende de**: nenhum
- **Spec-ref**: §4 cenário 3, §5, §9
- **Detalhes**: Adicionar constantes:
  - `internal const string AccountancyId = "accountancyId";`
  - `internal const string GetInviteLink = $"{BaseUri}/accountancies/{{{AccountancyId}:guid}}/invite-link";`
  - `internal const string RegenerateInviteLink = $"{BaseUri}/accountancies/{{{AccountancyId}:guid}}/invite-link/regenerate";`
  - `internal const string GetInviteLinkInfo = $"{BaseUri}/invite-links/info";` (token via query string)
  - `internal const string RegisterByInviteLink = $"{BaseUri}/invite-links/register";`

---

### 30. Request DTO `GetInviteLinkByAccountancyRequest`

- **Camada**: Web
- **Tipo**: Endpoint
- **Ação**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/Requests/Queries/GetInviteLinkByAccountancyRequest.cs`
- **Depende de**: #29
- **Spec-ref**: §5
- **Detalhes**: `sealed class` com `[FromRoute(Name = IdentityRoutes.AccountancyId)] Guid AccountancyId`.

---

### 31. Request DTO `RegenerateInviteLinkRequest`

- **Camada**: Web
- **Tipo**: Endpoint
- **Ação**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/Requests/Commands/RegenerateInviteLinkRequest.cs`
- **Depende de**: #29
- **Spec-ref**: §9
- **Detalhes**: `sealed class` com `[FromRoute(Name = IdentityRoutes.AccountancyId)] Guid AccountancyId`.

---

### 32. Request DTO `GetInviteLinkInfoRequest`

- **Camada**: Web
- **Tipo**: Endpoint
- **Ação**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/Requests/Queries/GetInviteLinkInfoRequest.cs`
- **Depende de**: #29
- **Spec-ref**: §5 "página pública de registro"
- **Detalhes**: `sealed class` com `[FromQuery] string Token { get; set; } = string.Empty`.

---

### 33. Request DTO `RegisterByInviteLinkRequest`

- **Camada**: Web
- **Tipo**: Endpoint
- **Ação**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/Requests/Commands/RegisterByInviteLinkRequest.cs`
- **Depende de**: nenhum
- **Spec-ref**: §9 "usuário preenche nome, email e senha"
- **Detalhes**: `sealed class` com `string Token, FirstName, LastName, Email, Password, ConfirmPassword` — sem atributo `[FromBody]` (implícito em POST).

---

### 34. Endpoint `GetInviteLinkByAccountancyEndpoint`

- **Camada**: Web
- **Tipo**: Endpoint
- **Ação**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/InviteLinks/GetInviteLinkByAccountancyEndpoint.cs`
- **Depende de**: #13, #17, #28, #29, #30
- **Spec-ref**: §5 "link sempre visível na área de membros"
- **Detalhes**: `[HttpGet(IdentityRoutes.GetInviteLink)]`, `[Authorize(Roles = "AccountingAdmin")]`. Response: `InviteLinkResponse`. Status codes: 200, 400. SwaggerOperation em PT, tag `Tags.Identity.InviteLinks`.

---

### 35. Endpoint `RegenerateInviteLinkEndpoint`

- **Camada**: Web
- **Tipo**: Endpoint
- **Ação**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/InviteLinks/RegenerateInviteLinkEndpoint.cs`
- **Depende de**: #11, #18, #28, #29, #31
- **Spec-ref**: §9 "renovação manual"
- **Detalhes**: `[HttpPost(IdentityRoutes.RegenerateInviteLink)]`, `[Authorize(Roles = "AccountingAdmin")]`. Response: `InviteLinkResponse`. Status codes: 200, 400. SwaggerOperation em PT, tag `Tags.Identity.InviteLinks`.

---

### 36. Endpoint `GetInviteLinkInfoEndpoint`

- **Camada**: Web
- **Tipo**: Endpoint
- **Ação**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/InviteLinks/GetInviteLinkInfoEndpoint.cs`
- **Depende de**: #14, #19, #28, #29, #32
- **Spec-ref**: §5 "página de registro exibe nome da contabilidade"
- **Detalhes**: `[HttpGet(IdentityRoutes.GetInviteLinkInfo)]`, `[AllowAnonymous]`. Response: `InviteLinkInfoResponse`. Status codes: 200, 400, 404. SwaggerOperation em PT, tag `Tags.Identity.InviteLinks`.

---

### 37. Endpoint `RegisterByInviteLinkEndpoint`

- **Camada**: Web
- **Tipo**: Endpoint
- **Ação**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/InviteLinks/RegisterByInviteLinkEndpoint.cs`
- **Depende de**: #12, #20, #28, #29, #33
- **Spec-ref**: §4 cenário 3, §9
- **Detalhes**: `[HttpPost(IdentityRoutes.RegisterByInviteLink)]`, `[AllowAnonymous]`. Sem response body (`WithActionResult`). Status codes: 200, 400, 404, 409. SwaggerOperation em PT, tag `Tags.Identity.InviteLinks`.

---

### 38. Modificar `InviteUserEndpoint` — novo response type + autorização

- **Camada**: Web
- **Tipo**: Endpoint
- **Ação**: modificar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/InviteUserEndpoint.cs`
- **Depende de**: #9, #10, #22
- **Spec-ref**: §4 cenários 1 e 2, §5 "link aparece na tela", §7 "fluxos SystemAdmin exclusivos"
- **Detalhes**: `.WithActionResult<IdentifierResponse>` → `.WithActionResult<UserInviteCreatedResponse>`. Adicionar `[Authorize(Roles = "SystemAdmin")]`. Atualizar `[ProducesResponseType(typeof(UserInviteCreatedResponse), 200)]`.

---

## Checklist de registro

- [x] **`ConsumerConfiguration.cs`** — item #26: 4 chamadas `ConfigureEventReceiveEndpoint` para os eventos `InviteLinkGenerated`, `InviteLinkRegenerated`, `InviteLinkUsed`, `InviteLinkExpired`.
- [x] **`AddEventInteractors()`** — item #24: `.AddScoped<IProjectionAccountancyInviteLinkEventHandler, ProjectionAccountancyInviteLinkEventHandler>()`.
- [ ] **`MigrateDatabaseStartupTask`** — não tocar; sem `DbContext` novo (`IdentityDbContext` já registrado).
- [x] **`RegisterRecurringJobsStartupTask`** — item #27: `ScheduleRecurring("expire-invite-links", "0 * * * *", new ExpireInviteLinksCommand())`.
- [ ] **`PersistenceServiceInstaller`** — não tocar; `IIdentityProjection<>` já registrado como open generic e cobre `AccountancyInviteLink` automaticamente.

---

## Riscos e pontos de atenção

- **Token raw na projeção (item #4)** — persiste `Token` (raw) no MongoDB para o botão "Copiar". Decisão de produto consciente: 256 bits de entropia (32 bytes) torna força bruta inviável. O campo deve ser sanitizado nos logs — nunca logar o token raw.

- **GUIDv5 determinístico (item #3)** — pattern novo no repositório. A constante `NAMESPACE` deve ser definida uma vez no aggregate e documentada em comentário XML explicando a escolha. Os handlers chamam `AccountancyInviteLink.GetId(accountancyId)` — nunca recomputam o namespace localmente.

- **Breaking change em `InviteUserCommand` (item #10)** — o frontend consome `IdentifierResponse` hoje. Com a mudança para `UserInviteCreatedResponse`, o campo `Id` continua presente; o campo `InviteLink` é novo. Coordenar deploy ou versionar o endpoint antes de subir.

- **Cross-module read em `GetInviteLinkInfoHandler` (item #19)** — `IAccountancyProjection<Accountancy.Domain.Projection.ProjectionModel.Accountancy>` injetado em Identity.Application. Leitura pontual e somente leitura — sem acoplamento de escrita. Aceitável para MVP; candidato futuro a replicar `AccountancyName` dentro do próprio `AccountancyInviteLink` para eliminar a dependência.

- **Dois `AppendEventsAsync` em `RegisterByInviteLinkHandler` (item #20)** — `User` e `AccountancyInviteLink` são appendados em sequência, sem transação distribuída. Se o segundo falhar após o primeiro ter sido commitado, o `User` existirá mas o contador do link ficará desatualizado. Mitigação MVP: o handler falha com 500, o cliente tenta novamente; no segundo attempt, `User` já existe → `ConflictError`. O evento `InviteLinkUsed` perdido é recuperável por job de reconciliação futuro se necessário.

- **`Convention tests` (`EndpointConventionsTests`)** — os 4 endpoints novos (#34–#37) e o endpoint modificado (#38) precisam ter `Summary`, `Description`, `OperationId` e `Tags` em português e seguindo o padrão validado pelos testes. Verificar antes de subir PR.

- **Expiração e `RegisterUsage` concorrentes** — race: job expira o link no mesmo instante em que um usuário tenta se registrar. O aggregate resolve via invariante em `RegisterUsage` (`Status == Active && ExpiresAt > usedAt`); se o evento de expiração já foi aplicado, o `RegisterUsage` não avança. O event store garante consistência por conflito de versão.
