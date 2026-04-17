# Reference — Plan Template

Template do plano técnico produzido pela skill `spec-engineer`. O plano é
**persistido** em `specs/{feature}/plan.md` na raiz deste repositório, revisado
pelo humano e commitado junto com a implementação — fica como rastro histórico
ligando spec de produto → plano técnico → código. Este arquivo define a
**forma** que o `plan.md` gerado deve seguir.

---

## Estrutura

O plano tem cinco blocos, sempre nesta ordem:

1. **Header** — identificação e resumo
2. **Pré-condições** — o que precisa existir antes das fases rodarem
3. **Fases de implementação** — grupos de itens por camada, cada um com build gate
4. **Checklist de registro** — pontos de bootstrap a atualizar
5. **Riscos e pontos de atenção** — red flags e decisões pendentes

---

## 1. Header

```
# Plano — {nome da feature}

**Spec**:  specs/{feature}/spec.md @ product.societiza
**Logic**: specs/{feature}/logic.md @ product.societiza
**Módulos afetados**: {Accountancy, Identity, ...}
**Módulo novo?**: sim | não
**Resumo**: 1-2 linhas descrevendo a mudança em linguagem técnica.
```

## 2. Pré-condições

Tudo o que precisa estar pronto antes de começar a executar as fases. Ex.:

- Migration `AddInviteStatusColumn` aplicada no `IdentityDbContext`
- `DbContext` novo `BillingDbContext` registrado em `MigrateDatabaseStartupTask`
- Decisão sobre backfill da projection `UserProjection` (rebuild do zero vs. migração incremental)

Se não houver pré-condições, escreva explicitamente `Nenhuma`.

---

## 3. Fases de implementação

O plano é dividido em **fases por camada**. Cada fase deve terminar com
`dotnet build Societiza.sln` zero errors antes da próxima começar (regra
`build-gate`). Cada item dentro de uma fase usa numeração local (`#1`, `#2`...).
Referências cross-fase usam o formato `Fase X · #N`.

Fases disponíveis (inclua apenas as que a feature toca):

---

### Fase 0 — Scaffold _(apenas se módulo novo)_

Cria os 5 projetos `.csproj` e adiciona ao `Societiza.sln`. Sem lógica de negócio.

**Referência**: `.claude/skills/architecture/SKILL.md` (estrutura de módulos)

**Itens** _(formato abaixo)_

**Build gate**: `dotnet build Societiza.sln` — solução carrega sem erros de referência.

---

### Fase 1 — Domain

SmartEnums, Entities, Domain Events, Aggregate, Projection Models.

**Referência**: `references/domain-architecture.md`

**Itens** _(máx ~10)_

**Build gate**: `dotnet build Societiza.sln` — zero errors antes de avançar.

---

### Fase 2 — Shared

Commands, Email Commands, Queries, Response DTOs, Snapshot DTOs.

**Referência**: `references/shared-architecture.md`

**Itens** _(máx ~10)_

**Build gate**: `dotnet build Societiza.sln` — zero errors antes de avançar.

---

### Fase 3 — Application

Validators, Handlers (Command e Query), Projection Handlers, DI registration (`AddEventInteractors`).

**Referência**: `references/application-architecture.md`

**Itens** _(máx ~12)_

**Build gate**: `dotnet build Societiza.sln` — zero errors antes de avançar.

---

### Fase 4 — Persistence

DbContext, Projection context, EF Configurations, Migration.

**Referência**: `references/persistence-architecture.md`

**Itens** _(máx ~6)_

**Build gate**: `dotnet build Societiza.sln` — zero errors antes de avançar.

---

### Fase 5 — Infrastructure

ModuleInstaller, ServiceInstallers, ApplicationService.

**Referência**: `references/infrastructure-architecture.md`

**Itens** _(máx ~5)_

**Build gate**: `dotnet build Societiza.sln` — zero errors antes de avançar.

---

### Fase 6 — Web

Routes, Endpoints, Request DTOs, MassTransit Consumers, ConsumerConfiguration,
CrossModule services, Registration updates (`MigrateDatabaseStartupTask`, `Web.csproj`).

**Referência**: `references/web-architecture.md` + skill `/api-design`

**Itens** _(máx ~12)_

**Build gate**: `dotnet build Societiza.sln` — zero errors. Depois: `dotnet test Societiza.sln` — todos passando.

---

## Formato de cada item

Dentro de cada fase, os itens seguem este formato:

```
### {N}. {Título curto do artefato}

- **Camada**: Domain | Application | Infrastructure | Persistence | Shared | Web
- **Tipo**: Aggregate | Entity | ValueObject | DomainEvent | Command | Validator
           | Handler | Query | ProjectionModel | ProjectionHandler | Endpoint
           | ResponseDTO | Migration | EFConfiguration | ConsumerRegistration
           | EmailCommand | EmailHandler | RecurringJob | SmartEnum | Service
- **Ação**: criar | modificar
- **Caminho**: src/Modules/{Module}/{Module}.{Layer}/{subpath}/{File}.cs
- **Depende de**: #{N}  |  Fase X · #N  |  nenhum
- **Spec-ref**: seção/regra da spec ou do logic doc que justifica este item
             (ex.: `spec §2.1`, `logic RN-04`, `logic §Estados`, `logic §Edge cases`)
- **Detalhes**: 1-3 linhas descrevendo o que o artefato faz ou o que muda
```

### Regras de ordenação dentro de cada fase

- Domain: SmartEnums → Value Objects → leaf Entities → parent Entities → Domain Events → Aggregate → Projection Models
- Shared: Snapshot DTOs → Email Commands → Commands → Queries → Response DTOs
- Application: Service interfaces → Validators → Command Handlers → Query Handlers → Projection Handlers → DI registration
- Persistence: Schemas/Constants → DbContext → Projection context → EF Configurations → Migration
- Infrastructure: ApplicationService → ModuleInstaller → ServiceInstallers
- Web: Routes → CrossModule services → Endpoints → Consumers → ConsumerConfiguration → Registration updates

---

## 4. Checklist de registro

Bloco fixo — o plano **sempre** lista cada ponto abaixo, marcando quais
serão tocados e quais permanecem inalterados. Esquecer um destes é o erro
mais comum ao adicionar feature nova.

- [ ] `ConsumerConfiguration.cs` — `ConfigureEventReceiveEndpoint` por evento novo
- [ ] `AddEventInteractors()` — registro dos projection handlers novos
- [ ] `MigrateDatabaseStartupTask` — registro de `DbContext` novo (se houver)
- [ ] `RegisterRecurringJobsStartupTask` — registro de jobs recorrentes (se houver)
- [ ] `PersistenceServiceInstaller` — registro explícito de open generics (se aplicável)
- [ ] `Web.csproj` — referência aos projetos novos (se módulo novo)
- [ ] `CrossModuleServicesInstaller` — serviços cross-module (se aplicável)

Para cada item marcado, indique **qual arquivo** e **o que exatamente** será
adicionado.

---

## 5. Riscos e pontos de atenção

Qualquer coisa que o humano precisa decidir ou validar antes de aprovar:

- **Red flags encontrados na spec** — acoplamento direto entre módulos,
  leitura via PostgreSQL, validação no Domain, tipos não-primitivos em email
  command, SmartEnum sem `.Name` explícito no evento
- **Ambiguidades** — regras de negócio que a spec deixa em aberto
- **Backfill de projection** — se um projection model existente muda de
  schema, descrever a estratégia (rebuild, migração incremental, ignorar
  dados antigos)
- **Mudanças que saem do escopo da spec** — refactors oportunistas que
  seriam convenientes mas não estão pedidos; listar para o humano decidir
  se entram ou ficam de fora

---

## Exemplo mínimo

Fragmento de plano para uma spec fictícia "Convite de usuário por e-mail",
mostrando as fases 1 e 2 (não é exaustivo — apenas ilustra o formato):

```markdown
# Plano — User Invite by Email

**Spec**: specs/user-invite-email/spec.md @ product.societiza
**Módulos afetados**: Identity
**Módulo novo?**: não
**Resumo**: Adiciona fluxo de convite assíncrono: admin envia convite, sistema
dispara e-mail via Hangfire, convidado aceita e vira User.

## Pré-condições
Nenhuma.

## Fases de implementação

### Fase 1 — Domain

**Referência**: `references/domain-architecture.md`

#### 1. Domain event `UserInviteSent`
- **Camada**: Domain
- **Tipo**: DomainEvent
- **Ação**: criar
- **Caminho**: src/Modules/Identity/Identity.Domain/Events/DomainEvents.cs
- **Depende de**: nenhum
- **Spec-ref**: §2.1 "Admin envia convite"
- **Detalhes**: record com InviteId, Email, Role (string via .Name), ExpiresAt, Version

#### 2. Método `UserInvite.Send()`
- **Camada**: Domain
- **Tipo**: Aggregate (modificação)
- **Ação**: modificar
- **Caminho**: src/Modules/Identity/Identity.Domain/UserInvites/UserInvite.cs
- **Depende de**: #1
- **Spec-ref**: §2.1
- **Detalhes**: levanta UserInviteSent via RaiseEvent<T>(); When() hidrata Status via FromName()

**Build gate**: `dotnet build Societiza.sln` — zero errors antes de avançar.

---

### Fase 2 — Shared

**Referência**: `references/shared-architecture.md`

#### 1. Command `SendUserInviteEmailCommand`
- **Camada**: Shared
- **Tipo**: EmailCommand
- **Ação**: criar
- **Caminho**: src/Modules/Identity/Identity.Shared/Emails/SendUserInviteEmailCommand.cs
- **Depende de**: nenhum
- **Spec-ref**: §2.2 "E-mail de convite"
- **Detalhes**: sealed record, apenas primitivos (Guid InviteId, string Email, string InviterName)

#### 2. Command `SendInviteCommand`
- **Camada**: Shared
- **Tipo**: Command
- **Ação**: criar
- **Caminho**: src/Modules/Identity/Identity.Shared/Commands/SendInviteCommand.cs
- **Depende de**: nenhum
- **Spec-ref**: §2.1
- **Detalhes**: sealed record SendInviteCommand(Guid AccountancyId, string Email, string Role) : ICommand

**Build gate**: `dotnet build Societiza.sln` — zero errors antes de avançar.
```
