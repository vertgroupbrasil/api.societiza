# Rule — Architecture

Escopo: backend

## Nunca fazer

- Colocar lógica de negócio fora do Domain — nunca em handlers, nunca em endpoints
- Chamar `When()` diretamente — só é invocado internamente por `ApplyEvent()`
- Atribuir estado diretamente no aggregate fora de um `When()` — sempre via `RaiseEvent<T>()`
- Hardcodar `Version` num evento — sempre usar o parâmetro do delegate em `RaiseEvent<T>()`
- Usar `switch` ou `if` em `ApplyEvent()` — sempre `When(@event as dynamic)`
- Declarar constructor público em aggregate — usar `private` constructor + `Create()`
- Usar SmartEnum como campo de Domain Event — SmartEnum tem conversão implícita para `string`,
  mas o campo do evento deve receber o valor já convertido via `newStatus.Name` quando
  a conversão implícita não ocorre automaticamente pelo compilador
- Espalhar eventos em múltiplos arquivos — todos em `{Module}.Domain/Events/DomainEvents.cs`
- Ler do PostgreSQL em queries — leituras sempre via MongoDB projections
- Injetar repositório diretamente em endpoint — sempre via MediatR
- Retornar entidade de domínio em endpoint — sempre Response DTO de `{Module}.Shared/Response/`
- Usar `AddTransient` para `IIdentityApplicationService` — sempre `TryAddTransient`
- Registrar open generics via Scrutor — sempre explícito em `PersistenceServiceInstaller`
- Referenciar módulos entre si — módulos só se comunicam via domain events
- Incluir `/api` em route constants — o prefixo é adicionado globalmente no host
- Passar tipos não-primitivos para email commands — Hangfire não consegue serializar
- Validar no Domain — aggregates não lançam exceções nem retornam `Result` por violação de regra de negócio; toda validação é responsabilidade do handler

## Sempre fazer

- Ler `.claude/skills/architecture/SKILL.md` antes de qualquer `.cs` ou `.csproj`
- Registrar novos projection handlers em `AddEventInteractors()`
- Registrar novos eventos em `ConsumerConfiguration.cs` — um `ConfigureEventReceiveEndpoint` por evento
- Adicionar novos DbContexts em `MigrateDatabaseStartupTask`
- Adicionar novos jobs recorrentes em `RegisterRecurringJobsStartupTask`
- Criar duas EF configs por aggregate novo: `StoreEventConfiguration` + `SnapshotConfiguration`
- Prefixar toda rota com `v{version:apiVersion}/{module}`
- Declarar `[ProducesResponseType]` para todos os status codes possíveis no endpoint
- Usar railway pipeline em todo endpoint: `Result.Create → Map → Bind → Match`

## Razão

O projeto segue DDD, CQRS e Event Sourcing de forma estrita. Cada regra acima
protege uma garantia do sistema: isolamento de módulos, reconstitução correta
de agregados, consistência entre event store e projections, e serialização
confiável de jobs.