# Plano — Identity Invitation Links

**Spec**: `specs/identity-invitation-links/spec.md` @ `vertgroupbrasil/product.societiza`
**Logic**: `specs/identity-invitation-links/logic.md` @ `vertgroupbrasil/product.societiza`
**Módulos afetados**: Identity (write + read), Accountancy (read via cross-module)
**Módulo novo?**: não
**Resumo**: Substitui o convite individual por e-mail por **links de convite**
reutilizáveis (`InvitationLink` — novo aggregate no Identity). Cada link carrega
tipo (SA→SA, SA→AA, AA→AA, AA→AE), contabilidade alvo quando aplicável, limite
de usos e validade de 7 dias. O uso é consumido pelo cadastro do convidado
(`Register`) em operação atômica com incremento de contador; o link
transita para `Exhausted` quando `uses == maxUses`, `Expired` por tempo ou
`Revoked` por admin. Unicidade de link ativo por escopo (tipo + contabilidade)
impõe-se no handler e na projection. **O fluxo `UserInvite` é removido
completamente neste ciclo — migrations destrutivas incluídas.**

---

## Pré-condições

- **O fluxo `UserInvite` é removido completamente**: todos os handlers,
  validators, endpoints, projection handler, job recorrente, eventos de domínio,
  aggregate `UserInvite`, tabelas EF (`UserInviteStoreEvents`, `UserInviteSnapshots`)
  e o campo `InviteId` em `UserRegistrationCompleted` são deletados. Migrations
  destrutivas serão geradas.
- Cadastro passa a ser **exclusivamente via link** (`Register`). Não há outro
  ponto de entrada.
- `.env.local` exportado antes de `dotnet ef` (regra já conhecida em
  `.claude/skills/infrastructure/SKILL.md` §EF).

---

## Fases de implementação

### Fase 1 — Domain

**Referência**: `.claude/skills/architecture/references/domain-architecture.md`

#### 1. SmartEnum `InvitationLinkStatus`
- **Camada**: Domain
- **Tipo**: SmartEnum
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Domain/Enumerations/InvitationLinkStatus.cs`
- **Depende de**: nenhum
- **Spec-ref**: `logic §2 Estados e transições` (Ativo / Esgotado / Expirado / Revogado)
- **Detalhes**: `Active = 0`, `Exhausted = 1`, `Expired = 2`, `Revoked = 3`. Mesmo padrão
  de `InviteStatus` (já existente): `class` com `SystemTextJson`-friendly ctor,
  implicit converters `string`/`int`/name/value, subclasses `private` por valor.

#### 2. Domain events da família `InvitationLink` em `DomainEvents.cs`
- **Camada**: Domain
- **Tipo**: DomainEvent
- **Ação**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Domain/Events/DomainEvents.cs`
- **Depende de**: #1
- **Spec-ref**: `logic §2 Estados e transições`, `logic §3 Regras`
- **Detalhes**: novos `sealed record`s, na região `#region InvitationLink Events`:
  - `InvitationLinkCreated(Guid Id, string TargetRole, string Status, Guid? AccountancyId, Guid CreatedByUserId, string TokenHash, int MaxUses, DateTimeOffset ExpiresAt, DateTimeOffset CreatedAt, ulong Version)`
  - `InvitationLinkConsumed(Guid Id, int Uses, Guid ConsumedByUserId, DateTimeOffset ConsumedAt, DateTimeOffset UpdatedAt, ulong Version)` — apenas incrementa `uses` sem mudar status
  - `InvitationLinkExhausted(Guid Id, string Status, DateTimeOffset UpdatedAt, ulong Version)`
  - `InvitationLinkExpired(Guid Id, string Status, DateTimeOffset UpdatedAt, ulong Version)`
  - `InvitationLinkRevoked(Guid Id, string Status, Guid RevokedByUserId, DateTimeOffset UpdatedAt, ulong Version)`
  - Todos: `sealed record ... : Message, IDomainEvent`. Tipos apenas primitivos.
    `Status` e `TargetRole` como `string` (`.Name`). Nunca SmartEnum no payload.

#### 3. Aggregate `InvitationLink`
- **Camada**: Domain
- **Tipo**: Aggregate
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Domain/Aggregates/InvitationLink.cs`
- **Depende de**: #1, #2
- **Spec-ref**: `logic §2 Estados`, `logic §3 Regras`, `spec §1 Visão da feature`
- **Detalhes**: `public class InvitationLink : AggregateRoot`. Propriedades com
  `private set`: `UserRole TargetRole`, `Guid? AccountancyId`, `Guid CreatedByUserId`,
  `string TokenHash`, `int MaxUses`, `int Uses`, `InvitationLinkStatus Status`,
  `DateTimeOffset ExpiresAt`, `Guid? RevokedByUserId`.
  - `public static InvitationLink Create(UserRole targetRole, Guid? accountancyId, Guid createdByUserId, string tokenHash, int maxUses)` — levanta `InvitationLinkCreated` com
    `ExpiresAt = UtcNow + 7d`, `Status = Active`, `Uses = 0`, `targetRole.Name`.
  - `public Result Consume(Guid userId)` — verifica invariantes **via eventos**:
    se não `Active` ou `Uses == MaxUses` ou `ExpiresAt <= UtcNow`, retorna `Result.Failure`
    (idempotente a corridas: validações de tempo/status dentro do aggregate **atuam como
    invariante**, não como regra de negócio — conforme `memory/feedback_domain_validation.md`
    as regras vêm pré-validadas pelo handler; aqui usamos apenas para a última defesa
    atômica).
    Em sucesso: `RaiseEvent<InvitationLinkConsumed>(Uses + 1, userId, now, now, v)`;
    se novo `Uses == MaxUses` **no mesmo fluxo**, levanta `InvitationLinkExhausted` em seguida.
  - `public void Expire()` — `RaiseEvent<InvitationLinkExpired>(Expired.Name, now, v)`.
  - `public void Revoke(Guid revokedByUserId)` — `RaiseEvent<InvitationLinkRevoked>(Revoked.Name, revokedByUserId, now, v)`.
  - `public bool IsUsable()` — `Status == Active && Uses < MaxUses && ExpiresAt > UtcNow`.
  - `protected override void ApplyEvent(IDomainEvent @event) => When(@event as dynamic);`
  - `When(InvitationLinkCreated)` hidrata tudo (`UserRole.FromName(e.TargetRole)`, `InvitationLinkStatus.FromName(e.Status)`).
  - `When(InvitationLinkConsumed)` incrementa `Uses`, atualiza `UpdatedAt`.
  - `When(InvitationLinkExhausted)` seta `Status = Exhausted`, `UpdatedAt`.
  - `When(InvitationLinkExpired)` seta `Status = Expired`, `UpdatedAt`.
  - `When(InvitationLinkRevoked)` seta `Status = Revoked`, `RevokedByUserId`, `UpdatedAt`.

#### 4. Evento `UserRegistrationCompleted` — remover `InviteId`, adicionar `InvitationLinkId`
- **Camada**: Domain
- **Tipo**: DomainEvent
- **Ação**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Domain/Events/DomainEvents.cs`
- **Depende de**: #2
- **Spec-ref**: `spec §5 "métricas de onboarding"`, `logic §Conta criada via link`
- **Detalhes**: remove o campo `Guid InviteId` (legado UserInvite) e adiciona `Guid InvitationLinkId` (não-nullable — todo cadastro novo vem obrigatoriamente de um link). Usuário seminal (primeiro SystemAdmin) usará `Guid.Empty` como sentinela, se necessário, mas não há mais campo `InviteId`.

#### 5. Aggregate `User` — refatorar `Create()` para `invitationLinkId`
- **Camada**: Domain
- **Tipo**: Aggregate (modificação)
- **Ação**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Domain/Aggregates/User.cs`
- **Depende de**: #4
- **Spec-ref**: `logic §Conta criada via link`
- **Detalhes**: Remove o método `Create(inviteId, ...)` legado e substitui por `public static User Create(Guid invitationLinkId, string firstName, string lastName, string email, string passwordHash, UserRole role, Guid? accountancyId)`. Levanta `UserRegistrationCompleted` com `InvitationLinkId = invitationLinkId`. `When(UserRegistrationCompleted)` atualiza para ler `InvitationLinkId` em vez de `InviteId`.

#### 6. Projection model `InvitationLink`
- **Camada**: Domain
- **Tipo**: ProjectionModel
- **Ação**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Domain/Projection/ProjectionModel.cs`
- **Depende de**: #1
- **Spec-ref**: `logic §4 Pontos de integração` (frontend precisa de status, uses, maxUses, expiresAt, targetRole, accountancyId, createdByUserId)
- **Detalhes**: novo `record InvitationLink(Guid Id, string TargetRole, string Status, Guid? AccountancyId, Guid CreatedByUserId, string TokenHash, int MaxUses, int Uses, DateTimeOffset ExpiresAt, Guid? RevokedByUserId, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt, bool IsDeleted = false) : IProjectionModel`. Collection name `InvitationLink`.

#### 7. Projection model `User` — adicionar `InvitationLinkId`
- **Camada**: Domain
- **Tipo**: ProjectionModel (modificação)
- **Ação**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Domain/Projection/ProjectionModel.cs`
- **Depende de**: #4
- **Spec-ref**: `spec §8 Decisões` "Todo usuário que entra por um link carrega referência ao link"
- **Detalhes**: adiciona `Guid? InvitationLinkId` ao record `User`, antes de `IsDeleted`. Permite `null` para o usuário seminal (primeiro SystemAdmin).

**Build gate**: `dotnet build Societiza.sln` — zero errors antes de avançar.

---

### Fase 2 — Shared

**Referência**: `.claude/skills/architecture/references/shared-architecture.md`

#### 1. Command `CreateInvitationLinkCommand`
- **Camada**: Shared
- **Tipo**: Command
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Commands/InvitationLinks/CreateInvitationLinkCommand.cs`
- **Depende de**: nenhum
- **Spec-ref**: `logic §1 Fluxos por perfil`
- **Detalhes**: `sealed record CreateInvitationLinkCommand(string TargetRole, Guid? AccountancyId, int? MaxUses, Guid CurrentUserId, string CurrentUserRole, Guid? CurrentUserAccountancyId) : ICommand<CreateInvitationLinkResponse>`. `TargetRole` é a role que o invitado vai receber ao se cadastrar. `AccountancyId` e identidade do caller vêm do endpoint (do `ICurrentUserService`), **não do payload HTTP**. `MaxUses` é obrigatório apenas quando `TargetRole = AccountancyEmployee`; para os demais o handler fixa `1`.

#### 2. Response DTO `CreateInvitationLinkResponse`
- **Camada**: Shared
- **Tipo**: ResponseDTO
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Response/InvitationLinks/CreateInvitationLinkResponse.cs`
- **Depende de**: nenhum
- **Spec-ref**: `spec §4 "link pronto para copiar"`
- **Detalhes**: `sealed record CreateInvitationLinkResponse(Guid Id, string Token, string Url, DateTimeOffset ExpiresAt)`. O raw token só existe neste retorno (e no DB fica apenas o hash).

#### 3. Command `RevokeInvitationLinkCommand`
- **Camada**: Shared
- **Tipo**: Command
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Commands/InvitationLinks/RevokeInvitationLinkCommand.cs`
- **Depende de**: nenhum
- **Spec-ref**: `logic §1 "Revogando um link"`, `logic §3` (quem pode revogar)
- **Detalhes**: `sealed record RevokeInvitationLinkCommand(Guid InvitationLinkId, Guid CurrentUserId, string CurrentUserRole, Guid? CurrentUserAccountancyId) : ICommand`.

#### 4. Command `RegisterCommand`
- **Camada**: Shared
- **Tipo**: Command
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Commands/InvitationLinks/RegisterCommand.cs`
- **Depende de**: nenhum
- **Spec-ref**: `logic §Convidado (sem conta na Societiza)`
- **Detalhes**: `sealed record RegisterCommand(string Token, string FirstName, string LastName, string Email, string Password, string ConfirmPassword) : ICommand<IdentifierResponse>`. Cadastro via link → retorna id do usuário criado.

#### 5. Query `GetInvitationLinkByTokenQuery`
- **Camada**: Shared
- **Tipo**: Query
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Queries/InvitationLinks/GetInvitationLinkByTokenQuery.cs`
- **Depende de**: #6
- **Spec-ref**: `logic §4 "Se o link acessado pelo convidado é válido (ativo) ou não, com o motivo"`
- **Detalhes**: `sealed record GetInvitationLinkByTokenQuery(string Token) : IQuery<InvitationLinkPublicResponse>`. Usado pelo frontend **antes** do cadastro para decidir se mostra o formulário ou a mensagem de erro.

#### 6. Response DTO `InvitationLinkPublicResponse`
- **Camada**: Shared
- **Tipo**: ResponseDTO
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Response/InvitationLinks/InvitationLinkPublicResponse.cs`
- **Depende de**: nenhum
- **Spec-ref**: `logic §4` (frontend precisa saber status e motivo)
- **Detalhes**: `sealed record InvitationLinkPublicResponse(string TargetRole, string Status, Guid? AccountancyId)`. Minimal — nenhum dado que vaze identificadores sensíveis (não expõe `CreatedByUserId`, `Uses`/`MaxUses`). `TargetRole` é o `UserRole.Name` do papel que o convidado receberá ao se cadastrar. O frontend só precisa saber o que mostrar; o backend é autoridade na validação.

#### 7. Query `ListInvitationLinksQuery`
- **Camada**: Shared
- **Tipo**: Query
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Queries/InvitationLinks/ListInvitationLinksQuery.cs`
- **Depende de**: #8
- **Spec-ref**: `spec §1 "visão default mostra apenas links ativos"`, `logic §1 "Visualizando links"`, `logic §4 "Isolamento multi-tenant na listagem"`
- **Detalhes**: `sealed record ListInvitationLinksQuery(bool OnlyActive, Guid? AccountancyId, Guid CurrentUserId, string CurrentUserRole, Guid? CurrentUserAccountancyId) : IQuery<List<InvitationLinkDetailResponse>>`. `OnlyActive == true` é a visão default; `false` retorna histórico (inclui terminais). `AccountancyId` é filtro para SystemAdmin navegando no contexto de uma contabilidade (SA→AA).

#### 8. Response DTO `InvitationLinkDetailResponse`
- **Camada**: Shared
- **Tipo**: ResponseDTO
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Response/InvitationLinks/InvitationLinkDetailResponse.cs`
- **Depende de**: nenhum
- **Spec-ref**: `logic §4` (frontend precisa de todos os campos listados)
- **Detalhes**: `sealed record InvitationLinkDetailResponse(Guid Id, string TargetRole, string Status, Guid? AccountancyId, Guid CreatedByUserId, int MaxUses, int Uses, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, DateTimeOffset? UpdatedAt)`. `TargetRole` é o `UserRole.Name` do aggregate. Não inclui `TokenHash` (nunca sai do backend).

#### 9. Query `ListUsersByInvitationLinkQuery`
- **Camada**: Shared
- **Tipo**: Query
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Queries/InvitationLinks/ListUsersByInvitationLinkQuery.cs`
- **Depende de**: #10
- **Spec-ref**: `spec §1 "junto com a lista de quem entrou por ele"`, `logic §4` "Lista de usuários que entraram por aquele link"
- **Detalhes**: `sealed record ListUsersByInvitationLinkQuery(Guid InvitationLinkId, Guid CurrentUserId, string CurrentUserRole, Guid? CurrentUserAccountancyId) : IQuery<List<InvitationLinkUserResponse>>`.

#### 10. Response DTO `InvitationLinkUserResponse`
- **Camada**: Shared
- **Tipo**: ResponseDTO
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Shared/Response/InvitationLinks/InvitationLinkUserResponse.cs`
- **Depende de**: nenhum
- **Spec-ref**: `logic §4` "Lista de usuários que entraram por aquele link (nome e data de entrada)"
- **Detalhes**: `sealed record InvitationLinkUserResponse(Guid UserId, string FirstName, string LastName, string Email, DateTimeOffset JoinedAt)`.

**Build gate**: `dotnet build Societiza.sln` — zero errors antes de avançar.

---

### Fase 3 — Application

**Referência**: `.claude/skills/architecture/references/application-architecture.md`

#### 1. Interface cross-module `IAccountancyExistenceService`
- **Camada**: Application
- **Tipo**: Service (interface)
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/Services/CrossModule/IAccountancyExistenceService.cs`
- **Depende de**: nenhum
- **Spec-ref**: `logic §1 "Criando um link SA → AA"` (a contabilidade vem do contexto de navegação; o handler precisa confirmar que ela existe antes de emitir o link)
- **Detalhes**: `public interface IAccountancyExistenceService { Task<bool> ExistsAsync(Guid accountancyId, CancellationToken ct); }`. Implementação vive em `src/Web/Services/CrossModule/AccountancyExistenceService.cs` (Fase 6) — padrão anti-corrupção documentado em `memory/feedback_cross_module_pattern.md`.

#### 2. Validator `CreateInvitationLinkValidator`
- **Camada**: Application
- **Tipo**: Validator
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Validators/InvitationLinks/CreateInvitationLinkValidator.cs`
- **Depende de**: Fase 2 · #1
- **Spec-ref**: `logic §3 Regras`, `logic §5 "maxUses zero ou negativo"`
- **Detalhes**: valida apenas a **forma** do comando:
  - `TargetRole`: não vazio, deve existir em `UserRole` (`UserRole.TryFromName`).
  - `MaxUses`: se informado, deve ser `>= 1`. Obrigatoriedade/conformidade por combinação caller → TargetRole (ex.: `AccountancyAdmin → AccountancyEmployee` requer `MaxUses`; `* → SystemAdmin` fixa `MaxUses = 1`) fica **no handler** (regra de negócio, não forma).
  - `AccountancyId`: quando exigido, deve ser `!= Guid.Empty` — mas **quem exige** é o handler (regra cruzada com `TargetRole` e com o role do caller).
  - `CurrentUserId`, `CurrentUserRole`: não vazios (defesa em profundidade).

#### 3. Handler `CreateInvitationLinkHandler`
- **Camada**: Application
- **Tipo**: Handler
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Commands/InvitationLinks/CreateInvitationLinkHandler.cs`
- **Depende de**: Fase 1 · #3, Fase 2 · #1, #2, #3 (deste grupo: #1, #2)
- **Spec-ref**: `logic §1`, `logic §3`, `logic §5 "Tentativa de criar segundo link ativo no mesmo escopo"`, `logic §5 "Criação simultânea por dois admins do mesmo tenant"`
- **Detalhes**: `public class CreateInvitationLinkHandler(IIdentityApplicationService appService, IIdentityProjection<ProjectionModel.InvitationLink> linkProjection, IAccountancyExistenceService accountancyExistence, IOptions<FrontendOptions> frontend) : ICommandHandler<CreateInvitationLinkCommand, CreateInvitationLinkResponse>`.
  Ordem de validação no handler (cada passo retorna `Result.Failure` com `Error` específico):
  1. **Parse** de `callerRole = UserRole.FromName(cmd.CurrentUserRole)` e `targetRole = UserRole.FromName(cmd.TargetRole)`. Se falhar, `ValidationError`.
  2. **Matriz de autorização** `callerRole → targetRole`:
     - `SystemAdmin` pode criar links com `TargetRole ∈ {SystemAdmin, AccountancyAdmin}`.
     - `AccountancyAdmin` pode criar links com `TargetRole ∈ {AccountancyAdmin, AccountancyEmployee}`.
     - Qualquer outra combinação → `ForbiddenError`.
  3. **Resolução de `accountancyId`**:
     - `TargetRole == SystemAdmin`: deve ser `null`. Rejeita se vier valor.
     - `TargetRole == AccountancyAdmin` + caller `SystemAdmin`: vem do `cmd.AccountancyId`; valida existência via `accountancyExistence.ExistsAsync`; se não existe → `NotFoundError("Accountancy.NotFound")`.
     - `TargetRole ∈ {AccountancyAdmin, AccountancyEmployee}` + caller `AccountancyAdmin`: força `accountancyId = cmd.CurrentUserAccountancyId`, ignorando qualquer valor do cliente (isolamento multi-tenant).
  4. **Limite de usos**:
     - `TargetRole ∈ {SystemAdmin, AccountancyAdmin}`: `maxUses = 1`, fixo.
     - `TargetRole == AccountancyEmployee`: exige `cmd.MaxUses >= 1`, senão `ValidationError("InvitationLink.MaxUsesRequired")`.
  5. **Unicidade de escopo ativo** via projection: busca link ativo com mesmo `TargetRole` + `AccountancyId`; se existe → `ConflictError("InvitationLink.AlreadyActiveInScope", "Já existe um link ativo nesse escopo — revogue o atual antes de criar outro.")`.
  6. **Geração do token**: `rawToken = RandomNumberGenerator.GetBytes(32) → Base64`; `tokenHash = SHA256`.
  7. **`InvitationLink.Create(targetRole, accountancyId, ...)`** e `appService.AppendEventsAsync(link)`.
  8. **Retorno**: `Result.Success(new CreateInvitationLinkResponse(link.Id, rawToken, $"{frontend.BaseUrl}/auth/join?token={rawToken}", link.ExpiresAt))`.

#### 4. Validator `RevokeInvitationLinkValidator`
- **Camada**: Application
- **Tipo**: Validator
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Validators/InvitationLinks/RevokeInvitationLinkValidator.cs`
- **Depende de**: Fase 2 · #3
- **Spec-ref**: `logic §3`
- **Detalhes**: `InvitationLinkId != Guid.Empty`, caller identity fields não vazios.

#### 5. Handler `RevokeInvitationLinkHandler`
- **Camada**: Application
- **Tipo**: Handler
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Commands/InvitationLinks/RevokeInvitationLinkHandler.cs`
- **Depende de**: Fase 1 · #4, Fase 2 · #3
- **Spec-ref**: `logic §1 "Revogando"`, `logic §3` (SystemAdmin só revoga SA→SA; AccountancyAdmin revoga qualquer ativo da sua contabilidade), `logic §5 "Revogação de link já inativo"`
- **Detalhes**:
  1. `LoadAggregateAsync<InvitationLink>(cmd.InvitationLinkId)` → se não existe, `NotFoundError`.
  2. Se `link.Status != Active` → `ConflictError("InvitationLink.NotRevocable", "O link não está mais ativo.")`.
  3. **Autoridade por escopo** (o tipo do link é inferido por `link.TargetRole` combinado com `link.AccountancyId`):
     - Caller `SystemAdmin`: só pode revogar se `link.TargetRole == UserRole.SystemAdmin` (equivale ao antigo `SA→SA`). Spec §8 "SystemAdmin pode revogar apenas links SA→SA" reforça; `logic §3`: "Links AA→AA e AA→AE pertencem ao tenant. SystemAdmin não pode revogá-los." **Decisão**: seguir o `logic.md` — SystemAdmin revoga apenas links cujo `TargetRole == SystemAdmin`. Links com `TargetRole == AccountancyAdmin` criados por SystemAdmin (antigo `SA→AA`) são revogados pelos AAs do tenant de destino. **Este ponto vai para "Riscos e pontos de atenção" para confirmação humana.**
     - Caller `AccountancyAdmin`: pode revogar qualquer link cujo `link.AccountancyId == cmd.CurrentUserAccountancyId` **E** `link.TargetRole ∈ {UserRole.AccountancyAdmin, UserRole.AccountancyEmployee}` (cobre os antigos `AA→AA`, `AA→AE` e também os `SA→AA` cujo `AccountancyId` é o tenant do caller). Caso contrário `ForbiddenError`.
     - Demais roles: `ForbiddenError`.
  4. `link.Revoke(cmd.CurrentUserId)`.
  5. `AppendEventsAsync`.

#### 6. Handler `RegisterHandler`
- **Camada**: Application
- **Tipo**: Handler
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Commands/InvitationLinks/RegisterHandler.cs`
- **Depende de**: Fase 1 · #4, #6, Fase 2 · #4
- **Spec-ref**: `logic §Convidado (sem conta na Societiza)`, `logic §Convidado (com conta existente)`, `logic §5 "Race condition no último uso"`, `logic §5 "Link expirado sem varredura prévia"`
- **Detalhes**: `public sealed class ... : ICommandHandler<RegisterCommand, IdentifierResponse>`. Injeções: `IIdentityApplicationService`, `IIdentityProjection<ProjectionModel.InvitationLink>`, `IIdentityProjection<ProjectionModel.User>`, `IPasswordService`.
  1. **Lookup do link por token** via projection: `linkProjection.FindAsync(x => x.TokenHash == sha256(cmd.Token) && !x.IsDeleted)`. Se não → `NotFoundError("InvitationLink.NotFound")`.
  2. **Bloqueio de conta existente** (`logic §Convidado com conta existente`): `userProjection.FindAsync(x => x.Email == cmd.Email)`; se existe → `ConflictError("User.AlreadyExists", "Já existe uma conta com esse e-mail.")`.
  3. **Load do aggregate** `LoadAggregateAsync<InvitationLink>(linkProjection.Id)` (fonte de verdade).
  4. **Tentativa de consumo** via `link.Consume(userIdPlaceholder)` — porém ainda não temos o userId final. Refatorar: gerar `userId` via `Guid.NewGuid()` antes para referenciar no evento de consumo.
  5. Se `link.Consume` falha (estado inválido/expirado/esgotado) → retorna erro de negócio: `Result.Failure(new Error("InvitationLink.NotUsable", motivo))` com motivo distinguível (`Expired`/`Exhausted`/`Revoked`).
  6. Em sucesso:
     - `User.Create(link.Id, firstName, lastName, email, passwordHash, link.TargetRole, link.AccountancyId)` — o papel do novo usuário é `link.TargetRole` (campo `UserRole` diretamente no aggregate).
     - `appService.AppendEventsAsync(user)` e `appService.AppendEventsAsync(link)` — dois aggregates, duas transações sequenciais; ordem importa (link persiste depois para que o evento `InvitationLinkConsumed` saia após o `UserRegistrationCompleted`, garantindo que a projection do User consiga referenciar o link se necessário).
  7. Retorna `Result.Success(new IdentifierResponse(user.Id))`.

#### 7. Validator `RegisterValidator`
- **Camada**: Application
- **Tipo**: Validator
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Validators/InvitationLinks/RegisterValidator.cs`
- **Depende de**: Fase 2 · #4
- **Spec-ref**: `logic §Convidado (sem conta na Societiza)`
- **Detalhes**: `Token NotEmpty`, `Email NotEmpty + EmailAddress + MaxLength(150)`, `FirstName/LastName NotEmpty + MaxLength(100)`, `Password NotEmpty + MinLength(8) + MaxLength(100)`, `ConfirmPassword Equal(x => x.Password)`.

#### 8. Query handler `GetInvitationLinkByTokenHandler`
- **Camada**: Application
- **Tipo**: Handler (Query)
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Queries/InvitationLinks/GetInvitationLinkByTokenHandler.cs`
- **Depende de**: Fase 2 · #5, #6
- **Spec-ref**: `logic §4` (frontend precisa saber se o link é válido e motivo)
- **Detalhes**: `public class ... : IQueryHandler<GetInvitationLinkByTokenQuery, InvitationLinkPublicResponse>`. Hash do token → `linkProjection.FindAsync`. Se não existe ou `IsDeleted` → `NotFoundError`. Calcula **status efetivo em tempo real** (`logic §4` "Expiração calculada no momento da validação"): se `Status == Active` mas `ExpiresAt <= UtcNow`, retorna `Status = "Expired"` no response (sem disparar evento aqui — job de expiração cuida da transição persistida). Retorna `new InvitationLinkPublicResponse(TargetRole, effectiveStatus, AccountancyId)`.

#### 9. Query handler `ListInvitationLinksHandler`
- **Camada**: Application
- **Tipo**: Handler (Query)
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Queries/InvitationLinks/ListInvitationLinksHandler.cs`
- **Depende de**: Fase 2 · #7, #8
- **Spec-ref**: `logic §1 "Visualizando links da contabilidade"`, `logic §4 "Isolamento multi-tenant na listagem"`, `logic §5 "AccountancyAdmin tenta ver links de outra contabilidade"`
- **Detalhes**:
  - `AccountingAdmin`: força `accountancyId = cmd.CurrentUserAccountancyId` (ignora filtro vindo do cliente); retorna links com `x.AccountancyId == currentAA`.
  - `SystemAdmin` sem `AccountancyId`: retorna apenas links `SA→SA` (tela geral de convites do SystemAdmin — `spec §8`).
  - `SystemAdmin` com `AccountancyId`: retorna links do tenant daquela contabilidade (`AA→AA`, `AA→AE`, `SA→AA` daquele tenant).
  - Filtro `OnlyActive`: `x.Status == "Active"` + `x.ExpiresAt > UtcNow` (cálculo em tempo real, conforme `logic §4`).
  - Demais roles: `ForbiddenError`.
  - Ordena por `CreatedAt desc`. Map para `InvitationLinkDetailResponse`.

#### 10. Query handler `ListUsersByInvitationLinkHandler`
- **Camada**: Application
- **Tipo**: Handler (Query)
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Queries/InvitationLinks/ListUsersByInvitationLinkHandler.cs`
- **Depende de**: Fase 1 · #8, Fase 2 · #9, #10
- **Spec-ref**: `spec §1`, `logic §4`
- **Detalhes**: busca o link na projection. Autoriza acesso: `SystemAdmin` tem acesso aos links `SA→SA` e qualquer link onde `link.AccountancyId` pertence ao contexto que ele está visualizando (aqui, aceita sempre para SystemAdmin); `AccountingAdmin` só se `link.AccountancyId == currentUser.AccountancyId`; demais → `ForbiddenError`. Depois `userProjection.ListAsync(x => x.InvitationLinkId == linkId && !x.IsDeleted)`, ordena por `CreatedAt`, map para `InvitationLinkUserResponse(UserId, FirstName, LastName, Email, JoinedAt=CreatedAt)`.

#### 11. Projection handler — composite `IProjectionInvitationLinkEventHandler`
- **Camada**: Application
- **Tipo**: ProjectionHandler
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Events/ProjectionInvitationLinkEventHandler.cs`
- **Depende de**: Fase 1 · #3, #7
- **Spec-ref**: `logic §Estados e transições`
- **Detalhes**: composite interface `IProjectionInvitationLinkEventHandler : IEventHandler<InvitationLinkCreated>, IEventHandler<InvitationLinkConsumed>, IEventHandler<InvitationLinkExhausted>, IEventHandler<InvitationLinkExpired>, IEventHandler<InvitationLinkRevoked>`.
  - `Created`: `ReplaceInsertAsync(new ProjectionModel.InvitationLink(...))`.
  - `Consumed`: `FindAsync` → `existing with { Uses = @event.Uses, UpdatedAt }` → upsert. Log warning se `existing is null`.
  - `Exhausted`/`Expired`/`Revoked`: `existing with { Status = @event.Status, UpdatedAt, (RevokedByUserId no caso de Revoked) }`.
  - Todos wrap em try/catch + `throw new InvalidOperationException(...)` para disparar retry do MassTransit.

#### 12. Projection handler `ProjectionUserEventHandler` — propaga `InvitationLinkId`
- **Camada**: Application
- **Tipo**: ProjectionHandler (modificação)
- **Ação**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Events/ProjectionUserEventHandler.cs`
- **Depende de**: Fase 1 · #5, #8
- **Spec-ref**: `spec §8` "Todo usuário que entra por um link carrega referência ao link"
- **Detalhes**: ajustar o `Handle(UserRegistrationCompleted)` para passar `@event.InvitationLinkId` ao construir `ProjectionModel.User`. Campo nullable preserva compatibilidade com registros legados.

**Itens pós-gates — registros em `ServiceCollectionExtensions`:**

#### 13. Registrar novos projection handlers em `AddEventInteractors()`
- **Camada**: Application
- **Tipo**: DI registration
- **Ação**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Application/DependencyInjection/ServiceCollectionExtensions.cs`
- **Depende de**: #11
- **Spec-ref**: `logic §Estados e transições` + regra projeto "Todo projection handler registra em `AddEventInteractors`"
- **Detalhes**: adicionar `.AddScoped<IProjectionInvitationLinkEventHandler, ProjectionInvitationLinkEventHandler>()`.

**Build gate**: `dotnet build Societiza.sln` — zero errors antes de avançar.

---

### Fase 4 — Persistence

**Referência**: `.claude/skills/architecture/references/persistence-architecture.md`

#### 1. EF configuration `InvitationLinkStoreEventConfiguration`
- **Camada**: Persistence
- **Tipo**: EFConfiguration
- **Ação**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Persistence/Configurations/StoreEventConfiguration.cs`
- **Depende de**: Fase 1 · #4
- **Spec-ref**: convenção `/architecture` — todo aggregate tem `StoreEventConfiguration<T>`
- **Detalhes**: adicionar classe interna `InvitationLinkStoreEventConfiguration : StoreEventConfiguration<InvitationLink>` (vazia, herda tudo do Core).

#### 2. EF configuration `InvitationLinkSnapshotConfiguration`
- **Camada**: Persistence
- **Tipo**: EFConfiguration
- **Ação**: modificar
- **Caminho**: `src/Modules/Identity/Identity.Persistence/Configurations/SnapshotConfiguration.cs`
- **Depende de**: Fase 1 · #4
- **Spec-ref**: convenção `/architecture`
- **Detalhes**: adicionar classe interna `InvitationLinkSnapshotConfiguration : SnapshotConfiguration<InvitationLink>` (vazia).

#### 3. Migration EF `AddInvitationLinkAggregate`
- **Camada**: Persistence
- **Tipo**: Migration
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Persistence/Migrations/<timestamp>_AddInvitationLinkAggregate.cs`
- **Depende de**: #1, #2
- **Spec-ref**: convenção `/infrastructure` — toda aggregate nova requer migration do event store
- **Detalhes**: gerar via
  ```bash
  dotnet ef migrations add AddInvitationLinkAggregate \
    --project src/Modules/Identity/Identity.Persistence \
    --startup-project src/Web \
    --context IdentityDbContext
  ```
  Deve produzir tabelas `identity."InvitationLinkStoreEvents"` e `identity."InvitationLinkSnapshots"`.

#### 4. Índice único parcial — MongoDB (concorrência de unicidade)
- **Camada**: Persistence
- **Tipo**: Infrastructure — Mongo index
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Persistence/Projection/InvitationLinkIndexSetup.cs` **OU** executar como parte de startup task no Web (preferido). **Decisão**: criar no `IdentityProjectionDbContext` um método `EnsureInvitationLinkIndexesAsync(CancellationToken)` que cria índice parcial único:
  - `{ TargetRole: 1, AccountancyId: 1 }` com `partialFilterExpression: { Status: "Active" }`.
- **Depende de**: Fase 1 · #7
- **Spec-ref**: `logic §4 "Verificação atômica de unicidade"`, `logic §5 "Criação simultânea por dois admins do mesmo tenant"`
- **Detalhes**: `IIdentityProjectionDbContext` ganha método `EnsureIndexesAsync(CancellationToken)`. `IdentityProjectionDbContext` cria o índice parcial via `MongoCollection.Indexes.CreateOne(...)`. Este método é invocado por uma nova startup task `EnsureProjectionIndexesStartupTask` (Fase 6). O índice **é a** garantia atômica — o handler faz a checagem "soft" antes, mas se dois requests simultâneos passarem, o insert do segundo evento disparará erro de duplicate key no upsert da projection → handler captura, retorna `ConflictError`.

#### 5. Confirmar `IdentityDbContext` aplica configs do assembly
- **Camada**: Persistence
- **Tipo**: verificação
- **Ação**: nenhuma (já implementado)
- **Caminho**: `src/Modules/Identity/Identity.Persistence/Context/IdentityDbContext.cs`
- **Depende de**: #1, #2
- **Spec-ref**: convenção `/persistence`
- **Detalhes**: `modelBuilder.ApplyConfigurationsFromAssembly(AssemblyReference.Assembly)` já está em `OnModelCreating` — as classes aninhadas em `StoreEventConfiguration` e `SnapshotConfiguration` são descobertas automaticamente. **Sem alteração.**

**Build gate**: `dotnet build Societiza.sln` — zero errors antes de avançar. Após build, rodar a migration em ambiente local:
```bash
dotnet ef database update \
  --project src/Modules/Identity/Identity.Persistence \
  --startup-project src/Web \
  --context IdentityDbContext
```

---

### Fase 5 — Infrastructure

**Referência**: `.claude/skills/architecture/references/infrastructure-architecture.md`

#### 1. Nenhuma alteração em ServiceInstallers ou ModuleInstaller
- **Camada**: Infrastructure
- **Tipo**: verificação
- **Ação**: nenhuma
- **Caminho**: —
- **Depende de**: —
- **Spec-ref**: —
- **Detalhes**: A feature não introduz novos `ITransient`/`IScoped` que demandem registro via Scrutor, nem novos open generics. `AppendEventsAsync<InvitationLink>` resolve `StoreEvent<InvitationLink>` via DbSet dinâmico — já suportado pelo `ApplicationService`. A classe `IdentityApplicationService` permanece intacta.

**Build gate**: `dotnet build Societiza.sln` — zero errors antes de avançar.

---

### Fase 6 — Web

**Referência**: `.claude/skills/architecture/references/web-architecture.md` + skill `/api-design`

#### 1. Rotas novas em `IdentityRoutes`
- **Camada**: Web
- **Tipo**: Routes
- **Ação**: modificar
- **Caminho**: `src/Web/Endpoints/Routes/IdentityRoutes.cs`
- **Depende de**: nenhum
- **Spec-ref**: —
- **Detalhes**: adicionar:
  ```
  internal const string InvitationLinkId = "invitationLinkId";

  internal const string CreateInvitationLink    = $"{BaseUri}/invitation-link";
  internal const string ListInvitationLinks     = $"{BaseUri}/invitation-link";
  internal const string RevokeInvitationLink    = $"{BaseUri}/invitation-link/{{{InvitationLinkId}:guid}}/revoke";
  internal const string ListInvitationLinkUsers = $"{BaseUri}/invitation-link/{{{InvitationLinkId}:guid}}/users";
  internal const string GetInvitationLinkByToken = $"{BaseUri}/invitation-link/token/{{token}}";
  internal const string Register = $"{BaseUri}/invitation-link/register";
  ```

#### 2. Implementação cross-module `AccountancyExistenceService`
- **Camada**: Web
- **Tipo**: CrossModuleService
- **Ação**: criar
- **Caminho**: `src/Web/Services/CrossModule/AccountancyExistenceService.cs`
- **Depende de**: Fase 3 · #1
- **Spec-ref**: `logic §1 SA→AA`
- **Detalhes**: `public sealed class AccountancyExistenceService(ISender sender) : IAccountancyExistenceService` com `ExistsAsync(id, ct) => sender.Send(new GetAccountancyDetailByIdQuery(id)).IsSuccess`. Padrão idêntico a `WorkflowTemplateDataService`.

#### 3. Registrar `IAccountancyExistenceService` em `CrossModuleServicesInstaller`
- **Camada**: Web
- **Tipo**: DI registration
- **Ação**: modificar
- **Caminho**: `src/Web/ServiceInstallers/CrossModule/CrossModuleServicesInstaller.cs`
- **Depende de**: #2
- **Spec-ref**: memória `feedback_cross_module_pattern.md`
- **Detalhes**: adicionar `services.AddScoped<IAccountancyExistenceService, AccountancyExistenceService>();`.

#### 4. Request `CreateInvitationLinkRequest`
- **Camada**: Web
- **Tipo**: RequestDTO
- **Ação**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/InvitationLinks/Requests/Commands/CreateInvitationLinkRequest.cs`
- **Depende de**: nenhum
- **Spec-ref**: `logic §1`
- **Detalhes**: `sealed class` com `string TargetRole = ""`, `Guid? AccountancyId`, `int? MaxUses`. `TargetRole` carrega o `UserRole.Name` que o link emitirá. O `AccountancyId` aqui só faz sentido quando `SystemAdmin` está criando um link com `TargetRole == AccountancyAdmin` (antigo `SA→AA`) — em todos os outros casos o handler ignora o valor do payload.

#### 5. Request `RevokeInvitationLinkRequest`
- **Camada**: Web
- **Tipo**: RequestDTO
- **Ação**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/InvitationLinks/Requests/Commands/RevokeInvitationLinkRequest.cs`
- **Depende de**: #1
- **Spec-ref**: —
- **Detalhes**: `[FromRoute(Name = IdentityRoutes.InvitationLinkId)] public Guid InvitationLinkId { get; set; }`.

#### 6. Request `RegisterRequest`
- **Camada**: Web
- **Tipo**: RequestDTO
- **Ação**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/InvitationLinks/Requests/Commands/RegisterRequest.cs`
- **Depende de**: nenhum
- **Spec-ref**: `logic §Convidado`
- **Detalhes**: `string Token, Email, FirstName, LastName, Password, ConfirmPassword` — todos FromBody.

#### 7. Request `GetInvitationLinkByTokenRequest`
- **Camada**: Web
- **Tipo**: RequestDTO
- **Ação**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/InvitationLinks/Requests/Queries/GetInvitationLinkByTokenRequest.cs`
- **Depende de**: #1
- **Spec-ref**: `logic §4`
- **Detalhes**: `[FromRoute(Name = "token")] public string Token { get; set; } = "";`.

#### 8. Request `ListInvitationLinksRequest`
- **Camada**: Web
- **Tipo**: RequestDTO
- **Ação**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/InvitationLinks/Requests/Queries/ListInvitationLinksRequest.cs`
- **Depende de**: nenhum
- **Spec-ref**: `logic §1, §4`
- **Detalhes**: `[FromQuery] public bool OnlyActive { get; set; } = true;` + `[FromQuery] public Guid? AccountancyId { get; set; }`.

#### 9. Request `ListInvitationLinkUsersRequest`
- **Camada**: Web
- **Tipo**: RequestDTO
- **Ação**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/InvitationLinks/Requests/Queries/ListInvitationLinkUsersRequest.cs`
- **Depende de**: #1
- **Spec-ref**: `spec §1`
- **Detalhes**: `[FromRoute(Name = IdentityRoutes.InvitationLinkId)] public Guid InvitationLinkId { get; set; }`.

#### 10. Endpoints (6 endpoints)
- **Camada**: Web
- **Tipo**: Endpoint
- **Ação**: criar
- **Caminho**: `src/Web/Endpoints/Modules/Identity/InvitationLinks/*.cs`
- **Depende de**: #1 a #9, Fase 3 completa
- **Spec-ref**: `logic §1`, `logic §4`, `spec §1`
- **Detalhes**: todos seguindo `/api-design`. Lista:
  | Endpoint | Rota | Verb | Authorize | Tag |
  |---|---|---|---|---|
  | `CreateInvitationLinkEndpoint` | `CreateInvitationLink` | POST | `[Authorize(Policy = IdentityPolicies.CanManageInvites)]` | `Identity.Invites` |
  | `RevokeInvitationLinkEndpoint` | `RevokeInvitationLink` | POST | `[Authorize(Policy = IdentityPolicies.CanManageInvites)]` | `Identity.Invites` |
  | `ListInvitationLinksEndpoint` | `ListInvitationLinks` | GET | `[Authorize(Policy = IdentityPolicies.CanManageInvites)]` | `Identity.Invites` |
  | `ListInvitationLinkUsersEndpoint` | `ListInvitationLinkUsers` | GET | `[Authorize(Policy = IdentityPolicies.CanManageInvites)]` | `Identity.Invites` |
  | `GetInvitationLinkByTokenEndpoint` | `GetInvitationLinkByToken` | GET | **público** | `Identity.Invites` |
  | `RegisterEndpoint` | `Register` | POST | **público** | `Identity.Invites` |

  Cada endpoint:
  - Injeta `ISender` + `ICurrentUserService` (nos protegidos).
  - No `.Map(...)` transforma `Request` → `Command`/`Query` preenchendo os campos de caller com `currentUser.UserId`, `currentUser.Role`, `currentUser.AccountancyId`.
  - `Summary`/`Description` em PT seguindo o template de 4 seções (`Propósito`, `**Regras**`, `**Efeitos**`, `**Autorização**`).
  - `[ProducesResponseType]` conforme tabela `/api-design §3`:
    - Create: `200 (CreateInvitationLinkResponse)`, `400`, `401`, `403`, `409`.
    - Revoke: `200`, `400`, `401`, `403`, `404`, `409`.
    - Register (`RegisterEndpoint`): `200 (IdentifierResponse)`, `400`, `404`, `409`.
    - List: `200 (List<InvitationLinkDetailResponse>)`, `400`, `401`, `403`.
    - List Users: `200 (List<InvitationLinkUserResponse>)`, `400`, `401`, `403`, `404`.
    - GetByToken: `200 (InvitationLinkPublicResponse)`, `400`, `404`.

#### 11. MassTransit consumer `ProjectionInvitationLinkConsumer`
- **Camada**: Web
- **Tipo**: Consumer
- **Ação**: criar
- **Caminho**: `src/Web/ServiceInstallers/EventBus/Consumers/IdentityModule/ProjectionInvitationLinkConsumer.cs`
- **Depende de**: Fase 3 · #11
- **Spec-ref**: convenção `/web` consumer por aggregate
- **Detalhes**: `public class ProjectionInvitationLinkConsumer(IProjectionInvitationLinkEventHandler h) : IConsumer<InvitationLinkCreated>, IConsumer<InvitationLinkConsumed>, IConsumer<InvitationLinkExhausted>, IConsumer<InvitationLinkExpired>, IConsumer<InvitationLinkRevoked>` com um `Consume` por evento delegando imediatamente ao handler.

#### 12. `ConsumerConfiguration` — 5 endpoints novos
- **Camada**: Web
- **Tipo**: ConsumerRegistration
- **Ação**: modificar
- **Caminho**: `src/Web/ServiceInstallers/EventBus/Consumers/IdentityModule/ConsumerConfiguration.cs`
- **Depende de**: #11
- **Spec-ref**: —
- **Detalhes**: na região `InvitationLink events` adicionar 5 chamadas:
  ```csharp
  rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionInvitationLinkConsumer, IdentityDomainEvent.InvitationLinkCreated>(registrationContext, MODULE_NAME);
  rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionInvitationLinkConsumer, IdentityDomainEvent.InvitationLinkConsumed>(registrationContext, MODULE_NAME);
  rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionInvitationLinkConsumer, IdentityDomainEvent.InvitationLinkExhausted>(registrationContext, MODULE_NAME);
  rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionInvitationLinkConsumer, IdentityDomainEvent.InvitationLinkExpired>(registrationContext, MODULE_NAME);
  rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionInvitationLinkConsumer, IdentityDomainEvent.InvitationLinkRevoked>(registrationContext, MODULE_NAME);
  ```

#### 13. Job recorrente `ExpireInvitationLinks`
- **Camada**: Application (definição) + Web (registro)
- **Tipo**: Job
- **Ação**: criar
- **Caminho**: `src/Modules/Identity/Identity.Application/UseCases/Jobs/ExpireInvitationLinksJob.cs` **e** `src/Web/StartupTasks/RegisterRecurringJobsStartupTask.cs`
- **Depende de**: Fase 1 · #4, #7
- **Spec-ref**: `logic §4 "Expiração calculada no momento da validação"` — job é **auxiliar**, mas serve para transicionar o status persistido
- **Detalhes**:
  - Parte Application: arquivo idêntico ao `ExpireInvitesJob.cs` existente, adaptado para o novo aggregate. `sealed record ExpireInvitationLinksCommand() : ICommand;`. Handler usa `IIdentityProjection<ProjectionModel.InvitationLink>` para listar `x.Status == "Active" && x.ExpiresAt < UtcNow`, faz `Parallel.ForEachAsync` (MaxDegreeOfParallelism = 5), carrega cada aggregate, chama `link.Expire()`, `AppendEventsAsync`.
  - Parte Web: adicionar `jobScheduler.ScheduleRecurring("expire-pending-invitation-links", "0 * * * *", new ExpireInvitationLinksCommand());` em `RegisterRecurringJobsStartupTask.ExecuteAsync`.

#### 14. Startup task `EnsureProjectionIndexesStartupTask`
- **Camada**: Web
- **Tipo**: StartupTask
- **Ação**: criar
- **Caminho**: `src/Web/StartupTasks/EnsureProjectionIndexesStartupTask.cs` + registro em `src/Web/ServiceInstallers/StartupTasks/StartupTasksServiceInstaller.cs`
- **Depende de**: Fase 4 · #4
- **Spec-ref**: `logic §4 "Verificação atômica de unicidade"`, `logic §5 "Criação simultânea"`
- **Detalhes**: `BackgroundService` que resolve `IIdentityProjectionDbContext` e chama `EnsureIndexesAsync(stoppingToken)`. Registrar com `services.AddHostedService<EnsureProjectionIndexesStartupTask>();`. Idempotente — Mongo ignora criação de índice existente.

**Build gate**: `dotnet build Societiza.sln` — zero errors. Depois: `dotnet test Societiza.sln` — todos passando. Em particular `EndpointConventionsTests` e qualquer teste afetado pelo schema `ProjectionModel.User`.

---

## Checklist de registro

- [x] **`ConsumerConfiguration.cs`** — 5 chamadas novas de `ConfigureEventReceiveEndpoint` para eventos do `InvitationLink` (Fase 6 · #12).
- [x] **`AddEventInteractors()`** — adiciona `AddScoped<IProjectionInvitationLinkEventHandler, ProjectionInvitationLinkEventHandler>()` (Fase 3 · #13).
- [ ] **`MigrateDatabaseStartupTask`** — sem alteração. Não há `DbContext` novo; `IdentityDbContext` já está registrado.
- [x] **`RegisterRecurringJobsStartupTask`** — adiciona job `"expire-pending-invitation-links"` (Fase 6 · #13).
- [x] **`PersistenceServiceInstaller`** — sem alteração em open generics (`IIdentityProjection<>` já registrado cobre `ProjectionModel.InvitationLink`).
- [ ] **`Web.csproj`** — sem alteração. Nenhum projeto novo.
- [x] **`CrossModuleServicesInstaller`** — adiciona `AddScoped<IAccountancyExistenceService, AccountancyExistenceService>()` (Fase 6 · #3).
- [x] **`StartupTasksServiceInstaller`** — adiciona `AddHostedService<EnsureProjectionIndexesStartupTask>()` (Fase 6 · #14).
- [x] **`IIdentityProjectionDbContext`** — adiciona `EnsureIndexesAsync(CancellationToken)` com criação do índice parcial único para `InvitationLink` (Fase 4 · #4).
- [x] **Migration EF** — `AddInvitationLinkAggregate` aplicada (Fase 4 · #3).

---

## Riscos e pontos de atenção

1. **Autoridade do SystemAdmin sobre `SA→AA` na revogação** — conflito aparente entre `spec §8` ("SystemAdmin pode revogar apenas links SA→SA") e a decisão de o link `SA→AA` habitar a contabilidade de destino (`spec §8`). O `logic §3` resolve: "Links AA→AA e AA→AE pertencem ao tenant. SystemAdmin não pode revogá-los." e "SystemAdmin pode revogar apenas links SA→SA." Portanto `SA→AA`, uma vez criado, é revogável apenas pelos AccountancyAdmins do tenant de destino. **O plano segue o logic**, mas há ambiguidade prática: se o tenant ainda não tem nenhum AA (o link SA→AA existe justamente para criar o primeiro AA), nenhum ator pode revogá-lo. **Confirmar com o humano antes de implementar.** Alternativa: permitir `SystemAdmin` revogar `SA→AA` como contingência.

2. **Race condition no consumo** (`logic §5`) — `InvitationLink.Consume` valida internamente as três condições (status/usos/expiração) antes de levantar o evento. Entretanto, a "atomicidade real" vem da sequência event store → snapshot → concorrência de versão (se dois processos carregam o aggregate na mesma versão e apelam a `AppendEventsAsync`, o segundo falhará por version mismatch). **Verificar que `ApplicationService.AppendEventsAsync` de fato faz controle de versão otimista**. Se não faz, o handler `RegisterViaInvitationLinkHandler` precisa de retry bounded (ou usar lock distribuído). Investigar na implementação.

4. **Unicidade por escopo (`logic §3`)** — a checagem "soft" no handler + índice parcial único na projection é boa prática, mas a **projection é eventually consistent** com o event store (há delay até o consumer processar o `InvitationLinkCreated`). Resultado: há uma janela de ~dezenas de ms onde duas criações simultâneas em escopos iguais podem passar a checagem soft e ambas persistirem eventos. A segunda só será rejeitada quando o consumer tentar inserir na projection. **Decisão proposta**: aceitar a janela. O estado "inconsistente" é raro e transitório (ambos links existem no event store; apenas um na projection; o outro será "silenciosamente órfão" até ser expirado pelo job). **Alternativa mais forte**: materializar a checagem de unicidade via tabela Postgres indexada (`identity."ActiveInvitationLinkScope"`) escrita na mesma transação do `AppendEventsAsync` — mas isso quebra a pureza event-sourced. Manter a proposta atual e marcar como item de monitoramento.

5. **`RegisterViaInvitationLinkHandler` — dois `AppendEventsAsync` sequenciais** — o handler cria `User` e consome `InvitationLink` em **duas transações separadas** (cada `AppendEventsAsync` abre sua própria `UnitOfWork`). Se a criação do User sucede mas o `AppendEventsAsync(link)` falha, teremos um User criado sem o contador do link incrementado. Mitigação aceitável: a próxima tentativa do convidado (se ele reenviar o formulário) vai falhar em "User.AlreadyExists", e o consumo ficará pendente — pequeno leak no contador. **Alternativa robusta**: encapsular os dois em uma `UnitOfWork` compartilhada — mas `AppendEventsAsync` não expõe isso hoje. **Confirmar se a mitigação é aceitável para o MVP**.

6. **Token no path (GetInvitationLinkByToken)** — colocar o token diretamente em `/invitation-link/token/{token}` expõe o token em logs de servidores, proxies e histórico do navegador. **Decisão proposta**: aceitar para MVP, com nota de segurança para evolução (mover para query string ou body). **Confirmar com o humano.**

7. **Convidado com conta existente em outra contabilidade** (`logic §5`) — o handler bloqueia apenas por `email == existing.Email`. Não diferencia "conta em outra contabilidade" de "conta nesta contabilidade". A mensagem retornada é genérica ("Já existe uma conta com esse e-mail"). Adequado ao MVP: multi-contabilidade é explicitamente fora do escopo.

8. **Projection `User.InvitationLinkId`** — a mudança de schema afeta todos os documentos existentes. MongoDB retornará `null` para o campo em registros antigos — campo é nullable no record C#, compatível. Não requer backfill.

9. **Nunca injetar `IEmailService`** — a spec explicitamente não envia e-mail para o convidado (`spec §6 "Não inclui disparo de e-mail para o convidado"`). **Nenhum email command, email handler, `jobScheduler.Enqueue` é planejado** nesta feature.

10. **Regras de autoridade espalhadas entre Validator e Handler** — a skill `/architecture` diz que validação de forma fica no Validator e regra de negócio no Handler. A matriz caller→TargetRole é regra de negócio (precondição sobre estado do mundo/role), portanto fica toda no handler. O Validator cobre apenas `TargetRole` ∈ `UserRole` válido (via `UserRole.TryFromName`) e `MaxUses >= 1` quando fornecido. **Seguir `memory/feedback_domain_validation.md` e `spec-engineer §Como ler o logic doc`**: nenhum guard de autoridade no Domain.

---

## Aguardando

**Aprovação humana explícita antes de iniciar a Fase 1.** Em particular, preciso de resposta nos pontos:
- Risco 1 (revogação de SA→AA),
- Risco 5 (dupla transação no registro via link),
- Risco 6 (token no path).

Os demais riscos são documentais; a implementação segue o que está descrito, a menos que o humano apontem mudança.
