# Command — new-feature

Implementação de nova feature end-to-end no backend do Societiza.
Segue a ordem obrigatória: Domain → Shared → Application → Persistence → Infrastructure → Web.

## Pré-condições

- [ ] Infraestrutura local rodando: `docker-compose -f Docker/docker-compose.Infrastructure.Development.yaml up -d`
- [ ] Build limpo: `dotnet build Societiza.sln`
- [ ] Módulo alvo identificado: Accountancy | Identity | WorkflowTemplate | novo
- [ ] Comportamento esperado definido — o que o aggregate deve fazer

## Inputs obrigatórios

- `{Feature}`: nome em PascalCase (ex: `RevokeUserInvite`)
- `{Module}`: módulo alvo (ex: `Identity`)
- `{Aggregate}`: aggregate afetado (ex: `UserInvite`)

## Passos

### 1. Ler skills antes de escrever qualquer código

- `.claude/skills/architecture/SKILL.md`

### 2. Domain

- [ ] Behavior method no aggregate — chama `RaiseEvent<T>()`, nunca atribui diretamente
- [ ] SmartEnum no evento: `.Name` explícito — `Status: InviteStatus.Revoked.Name`
- [ ] `sealed record` do evento em `{Module}.Domain/Events/DomainEvents.cs` com campo `ulong Version`
- [ ] `When()` handler no aggregate — usa `FromName()` para reconstituir SmartEnum
- [ ] Projection model atualizado em `{Module}.Domain/Projection/ProjectionModel.cs` se necessário

### 3. Shared

- [ ] Command em `{Module}.Shared/Commands/{Feature}Command.cs` — `sealed record : ICommand`
- [ ] Response DTO em `{Module}.Shared/Response/` se necessário — `sealed record`
- [ ] Email command em `{Module}.Shared/Emails/Send{Action}EmailCommand.cs` se necessário

### 4. Application

- [ ] Validator em `UseCases/Validators/{Feature}Validator.cs` — valida shape do command
- [ ] Handler em `UseCases/Commands/{Feature}Handler.cs`:
  - Verificações via projection (duplicate check, not found)
  - `LoadAggregateAsync<TAggregate>(id, ct)` para mutação
  - Chama behavior method no aggregate
  - `AppendEventsAsync(aggregate, ct)` para persistir
  - `jobScheduler.Enqueue(new Send{Action}EmailCommand(...))` se necessário
- [ ] Projection handler em `UseCases/Events/Projection{Aggregate}EventHandler.cs`:
  - Adicionar `IEventHandler<NovoEvento>` à interface
  - Implementar `Handle(NovoEvento, ct)` com `try/catch`
  - Usar `existing with { Campo = @event.Campo }` para updates
  - Registrar em `DependencyInjection/ServiceCollectionExtensions.cs` → `AddEventInteractors()`

### 5. Persistence — somente se novo aggregate

- [ ] `{Aggregate}StoreEventConfiguration : StoreEventConfiguration<TAggregate>`
- [ ] `{Aggregate}SnapshotConfiguration : SnapshotConfiguration<TAggregate>`
- [ ] Executar `.claude/commands/add-migration.md`

### 6. Web — Consumer

- [ ] Adicionar `IConsumer<NovoEvento>` ao consumer do módulo
- [ ] Adicionar `Consume(ConsumeContext<NovoEvento>)` — delega para event handler, sem lógica
- [ ] Registrar em `ConsumerConfiguration.cs`: `bus.ConfigureEventReceiveEndpoint<TConsumer, NovoEvento>(context, MODULE_NAME)`

### 7. Web — Endpoint

- [ ] Route constant em `Web/Endpoints/Routes/{Module}Routes.cs`
- [ ] Request DTO em `Web/Endpoints/Modules/{Module}/Requests/Commands/{Feature}Request.cs`
- [ ] Endpoint em `Web/Endpoints/Modules/{Module}/{Feature}Endpoint.cs`:
  - Herda `EndpointBaseAsync.WithRequest<TRequest>.WithActionResult`
  - Decorators: `[ApiVersion]`, `[Http{Verb}]`, `[ProducesResponseType]` x N, `[SwaggerOperation]`
  - Railway pipeline: `Result.Create → Map → Bind → Match`

### 8. Testes

- [ ] Ler `.claude/skills/testing/SKILL.md`
- [ ] Handler tests em `tests/Web.Tests/Modules/{Module}/Application/Commands/{Feature}HandlerTests.cs`
- [ ] Cobrir: cada conflito, caminho feliz com resultado, caminho feliz com `AppendEventsAsync` verificado
- [ ] Aggregate tests em `tests/Web.Tests/Modules/{Module}/Domain/Aggregates/` se novo comportamento no Domain

### 9. Validação final
```bash
dotnet build Societiza.sln
dotnet test Societiza.sln
```

## Output esperado

- Aggregate com novo comportamento e evento correspondente
- Handler funcional com validator
- Projection atualizada no MongoDB após evento
- Endpoint respondendo na rota correta com prefixo `/api`
- Testes cobrindo caminhos de erro e caminho feliz
- Build e testes passando

## Skills requeridas

- `.claude/skills/architecture/SKILL.md`
- `.claude/skills/architecture/references/checklist.md`
- `.claude/skills/domain-modeling/SKILL.md`
- `.claude/skills/testing/SKILL.md`