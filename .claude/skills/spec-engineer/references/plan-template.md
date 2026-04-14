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
2. **Pré-condições** — o que precisa existir antes dos itens rodarem
3. **Itens do plano** — lista enumerada e ordenada
4. **Checklist de registro** — pontos de bootstrap a atualizar
5. **Riscos e pontos de atenção** — red flags e decisões pendentes

---

## 1. Header

```
# Plano — {nome da feature}

**Spec**: specs/{feature}/spec.md @ product.societiza
**Módulos afetados**: {Accountancy, Identity, ...}
**Módulo novo?**: sim | não
**Resumo**: 1-2 linhas descrevendo a mudança em linguagem técnica.
```

## 2. Pré-condições

Tudo o que precisa estar pronto antes de começar a executar os itens. Ex.:

- Migration `AddInviteStatusColumn` aplicada no `IdentityDbContext`
- `DbContext` novo `BillingDbContext` registrado em `MigrateDatabaseStartupTask`
- Decisão sobre backfill da projection `UserProjection` (rebuild do zero vs. migração incremental)

Se não houver pré-condições, escreva explicitamente `Nenhuma`.

## 3. Itens do plano

Lista numerada e **ordenada por dependência**. Cada item segue o formato:

```
### {N}. {Título curto do artefato}

- **Camada**: Domain | Application | Infrastructure | Persistence | Shared | Web
- **Tipo**: Aggregate | Entity | ValueObject | DomainEvent | Command | Validator
           | Handler | Query | ProjectionModel | ProjectionHandler | Endpoint
           | ResponseDTO | Migration | EFConfiguration | ConsumerRegistration
           | EmailCommand | EmailHandler | RecurringJob | SmartEnum
- **Ação**: criar | modificar
- **Caminho**: src/Modules/{Module}/{Module}.{Layer}/{subpath}/{File}.cs
- **Depende de**: #{N}, #{M}   (ou "nenhum")
- **Spec-ref**: seção/regra da spec que justifica este item
- **Detalhes**: 1-3 linhas descrevendo o que o artefato faz ou o que muda
```

### Regras de ordenação

Os itens devem respeitar as dependências reais entre camadas e entre eventos:

1. **Domain antes de Application** — aggregate, domain event e SmartEnum vêm
   antes de qualquer command/handler que os use
2. **Shared antes de Application** — commands, validators, response DTOs e
   email commands vivem em Shared e são consumidos pelos handlers
3. **Migration antes de endpoint que depende dela** — se o endpoint lê/escreve
   coluna nova, a migration precisa aparecer antes
4. **EF Configurations junto ao aggregate** — `StoreEventConfiguration` e
   `SnapshotConfiguration` aparecem logo após o aggregate que configuram
5. **Projection model antes de query** — a query só existe se houver onde ler
6. **Domain event antes de projection handler** — o handler consome o evento,
   então o evento vem primeiro
7. **Consumer registration depois do evento** — `ConfigureEventReceiveEndpoint`
   é registrado depois que o evento existe
8. **Endpoint por último no fluxo** — só entra depois que command, handler,
   validator, response DTO e (quando aplicável) migration estão prontos
9. **Email command antes do handler de negócio que o enfileira** — o handler
   de negócio faz `Enqueue(new SendXxxEmailCommand(...))`, então o command
   e seu handler já precisam existir

## 4. Checklist de registro

Bloco fixo — o plano **sempre** lista cada ponto abaixo, marcando quais
serão tocados e quais permanecem inalterados. Esquecer um destes é o erro
mais comum ao adicionar feature nova.

- [ ] `ConsumerConfiguration.cs` — `ConfigureEventReceiveEndpoint` por evento novo
- [ ] `AddEventInteractors()` — registro dos projection handlers novos
- [ ] `MigrateDatabaseStartupTask` — registro de `DbContext` novo (se houver)
- [ ] `RegisterRecurringJobsStartupTask` — registro de jobs recorrentes (se houver)
- [ ] `PersistenceServiceInstaller` — registro explícito de open generics (se aplicável)

Para cada item marcado, indique **qual arquivo** e **o que exatamente** será
adicionado.

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
mostrando 4 itens ordenados (não é exaustivo — apenas ilustra o formato):

```
# Plano — User Invite by Email

**Spec**: specs/user-invite-email/spec.md @ product.societiza
**Módulos afetados**: Identity
**Módulo novo?**: não
**Resumo**: Adiciona fluxo de convite assíncrono: admin envia convite, sistema
dispara e-mail via Hangfire, convidado aceita e vira User.

## Pré-condições
Nenhuma.

## Itens

### 1. Domain event `UserInviteSent`
- **Camada**: Domain
- **Tipo**: DomainEvent
- **Ação**: criar
- **Caminho**: src/Modules/Identity/Identity.Domain/Events/DomainEvents.cs
- **Depende de**: nenhum
- **Spec-ref**: §2.1 "Admin envia convite"
- **Detalhes**: record com InviteId, Email, Role (string via .Name), ExpiresAt, Version

### 2. Método `UserInvite.Send()`
- **Camada**: Domain
- **Tipo**: Aggregate (modificação)
- **Ação**: modificar
- **Caminho**: src/Modules/Identity/Identity.Domain/UserInvites/UserInvite.cs
- **Depende de**: #1
- **Spec-ref**: §2.1
- **Detalhes**: levanta UserInviteSent via RaiseEvent<T>(); When() hidrata Status via FromName()

### 3. Command `SendUserInviteEmailCommand`
- **Camada**: Shared
- **Tipo**: EmailCommand
- **Ação**: criar
- **Caminho**: src/Modules/Identity/Identity.Shared/Emails/SendUserInviteEmailCommand.cs
- **Depende de**: nenhum
- **Spec-ref**: §2.2 "E-mail de convite"
- **Detalhes**: sealed record, apenas primitivos (Guid InviteId, string Email, string InviterName)

### 4. Handler `SendInviteCommandHandler`
- **Camada**: Application
- **Tipo**: Handler
- **Ação**: criar
- **Caminho**: src/Modules/Identity/Identity.Application/UseCases/UserInvites/SendInviteCommandHandler.cs
- **Depende de**: #2, #3
- **Spec-ref**: §2.1, §2.2
- **Detalhes**: valida precondições, carrega UserInvite, chama Send(), enfileira SendUserInviteEmailCommand via IJobSchedulerService
```