# Checklist de Implementação de Funcionalidade

> Use esta checklist toda vez que implementar uma nova funcionalidade de ponta a ponta.
> Siga a ordem abaixo — nunca pule camadas nem implemente fora de sequência.
> Para patterns detalhados em cada camada, leia o arquivo de referência correspondente.

---

## Antes de Começar

- Leia `references/domain-architecture.md` se a funcionalidade exigir novo Aggregate, Event ou Enumeration
- Leia `references/application-architecture.md` para padrões de Handler, Validator e Projection Handler
- Leia `references/shared-architecture.md` para estrutura de comando e DTO de resposta
- Leia `references/persistence-architecture.md` se a funcionalidade adicionar um novo Aggregate ao event store
- Leia `references/infrastructure-architecture.md` se a funcionalidade exigir novo registro de serviço

---

## Passo 1 — Domain

### Novo Aggregate
- [ ] Criar `{Module}.Domain/Aggregates/{AggregateName}.cs` herdando de `AggregateRoot`
- [ ] Construtor private
- [ ] Propriedades públicas com `private set`
- [ ] Método factory `Create()` — levanta o evento de criação via `RaiseEvent<T>()`
- [ ] Métodos de comportamento — cada um chama `RaiseEvent<T>()`, nunca atribui estado diretamente
- [ ] `ApplyEvent(IDomainEvent @event) => When(@event as dynamic)`
- [ ] Um `private void When(TEvent @event)` por Domain Event

### Novos Domain Events
- [ ] Adicionar `sealed record` a `{Module}.Domain/Events/DomainEvents.cs`
- [ ] Herda de `Message, IDomainEvent` — nesta ordem
- [ ] Todos os campos são primitivos — sem SmartEnum, sem tipos de Domain
- [ ] Campo `Version` presente em todo evento
- [ ] Campos SmartEnum convertidos para `string` via `.Name` ao levantar o evento

### Nova Enumeration
- [ ] Criar `{Module}.Domain/Enumerations/{EnumName}.cs`
- [ ] `abstract class` herdando de `SmartEnum<T>`
- [ ] Conversões implícitas para `string` e `int`
- [ ] Subclasse `private sealed` por valor com `int` explícito e estável

### Novo Projection Model
- [ ] Adicionar `record` a `{Module}.Domain/Projections/ProjectionModel.cs`
- [ ] Implementa `IProjectionModel`
- [ ] Todos os campos são primitivos — sem SmartEnum, sem tipos de Domain
- [ ] `IsDeleted` com default `false`

---

## Passo 2 — Shared

### Novo comando
- [ ] Criar `{Module}.Shared/Commands/{OperationName}Command.cs`
- [ ] `sealed record` implementando `ICommand` ou `ICommand<IdentifierResponse>`
- [ ] Todos os campos são primitivos — sem tipos de Domain, sem SmartEnums

### Novo comando de email
- [ ] Criar `{Module}.Shared/Emails/{OperationName}EmailCommand.cs`
- [ ] `sealed record` implementando `ICommand`
- [ ] Todos os campos são `string` — requisito de serialização do Hangfire

### Novo DTO de resposta
- [ ] Criar `{Module}.Shared/Response/{Resource}{Intent}Response.cs`
- [ ] `sealed record` — sem herança, sem interfaces
- [ ] Todos os campos são primitivos, `Guid`, `DateTimeOffset` ou variantes nullable

---

## Passo 3 — Application

### Novo Validator
- [ ] Criar `{Module}.Application/UseCases/Validators/{OperationName}Validator.cs`
- [ ] `sealed class` herdando de `AbstractValidator<TCommand>`
- [ ] Valida apenas o formato do comando — sem carregamento de aggregate ou projection
- [ ] Regras de estado (ex.: `Status == Pending`) pertencem ao Handler, não aqui

### Novo handler de comando
- [ ] Criar `{Module}.Application/UseCases/Commands/{OperationName}Handler.cs`
- [ ] Implementa `ICommandHandler<TCommand>` ou `ICommandHandler<TCommand, TResponse>`
- [ ] Use projection (`IIdentityProjection<T>`) para verificações de duplicidade e lookups
- [ ] Use `LoadAggregateAsync` para qualquer mutação
- [ ] Chame método de comportamento no aggregate — nunca mute estado diretamente
- [ ] Chame `AppendEventsAsync` após o método de comportamento
- [ ] Agende emails via `IJobSchedulerService.Enqueue()` — nunca envie diretamente

### Novo handler de projection
- [ ] Adicionar `IEventHandler<TEvent>` à interface composta em `ProjectionUserInviteEventHandler.cs`
- [ ] Adicionar método `Handle(TEvent @event, ...)` à classe handler
- [ ] Eventos de criação: chamar `ReplaceInsertAsync` com um novo record de projection
- [ ] Eventos de update: `FindAsync` → `existing with { ... }` → `ReplaceInsertAsync`
- [ ] Envolver todo corpo de `Handle` em `try/catch` — logar erro e relançar como `InvalidOperationException`
- [ ] Se `existing is null` em um handler de update: registrar warning e retornar (idempotência)
- [ ] Registrar o novo handler em `ServiceCollectionExtensions.AddEventInteractors()`

### Novo handler de email
- [ ] Criar `{Module}.Application/UseCases/Emails/{OperationName}EmailHandler.cs`
- [ ] Implementa `ICommandHandler<TEmailCommand>`
- [ ] Chama `IEmailService.SendAsync()` — assunto e corpo definidos aqui, não em `IEmailService`

### Novo handler de job
- [ ] Criar `{Module}.Application/UseCases/Jobs/{JobName}Job.cs`
- [ ] `Command` e `Handler` no mesmo arquivo
- [ ] Comando é `internal` — não exposto fora de Application
- [ ] Use projection para encontrar candidatos, event store para carregar e mutar aggregates
- [ ] Use `continue` em loops — não falhe o batch inteiro por um aggregate

---

## Passo 4 — Persistence

### Novo Aggregate no Event Store
- [ ] Adicionar `{AggregateName}StoreEventConfiguration : StoreEventConfiguration<TAggregate>` a `Configurations/`
- [ ] Adicionar `{AggregateName}SnapshotConfiguration : SnapshotConfiguration<TAggregate>` a `Configurations/`
- [ ] Rodar `dotnet ef migrations add {MigrationName}` para gerar as tabelas

---

## Passo 5 — Infrastructure

### Novo Service (registrado automaticamente)
- [ ] Implementar `ITransient` ou `IScoped` na classe — Scrutor vai registrá-la automaticamente
- [ ] Definir a interface em `{Module}.Application/Services/`
- [ ] Colocar a implementação em `{Module}.Infrastructure/Services/`

### Novo Registro de Genérico Aberto
- [ ] Adicionar `services.AddScoped(typeof(IFoo<>), typeof(Foo<>))` explícito a `PersistenceServiceInstaller`
- [ ] Nunca depender do Scrutor para genéricos abertos

---

## Passo 6 — Web (Consumers)

### Novo MassTransit Consumer Event

### Novo DTO de requisição
- [ ] Criar `src/Web/Endpoints/Modules/{Module}/Requests/{Commands|Queries}/{OperationName}Request.cs`
- [ ] Usar atributos `[FromRoute]`, `[FromBody]` ou `[FromQuery]` conforme necessário

### Novo Endpoint
- [ ] Criar `src/Web/Endpoints/Modules/{Module}/{OperationName}Endpoint.cs`
- [ ] Herda de `EndpointBaseAsync.WithRequest<TRequest>.WithActionResult` ou `.WithActionResult<TResponse>`
- [ ] Decorado com `[ApiVersion("1.0")]`, `[Http{Method}(route)]`, `[ProducesResponseType]`, `[SwaggerOperation]`
- [ ] Pipeline: `Result.Create(request).Map(...).Bind(...).Match(onSuccess, this.HandleFailure)`

---

## Passo 6 — Web (Consumers)

### Novo MassTransit Consumer Event
- [ ] Adicionar `IConsumer<TEvent>` à classe consumer existente para o aggregate (ex.: `ProjectionUserInviteConsumer`)
- [ ] Adicionar método `Consume(ConsumeContext<TEvent> context)` — delegar imediatamente para o event handler, sem lógica
- [ ] Adicionar `bus.ConfigureEventReceiveEndpoint<TConsumer, TEvent>(context, MODULE_NAME)` a `ConsumerConfiguration.cs`

---

## Passo 7 — Web (Endpoint)

### Nova Route
- [ ] Adicionar constante de rota a `src/Web/Endpoints/Routes/{Module}Routes.cs`
- [ ] Adicionar constante de nome de parâmetro se a rota tiver segmento `{param:guid}` — compartilhada com `[FromRoute(Name = ...)]`

### Novo Recurring Job
- [ ] Adicionar `jobScheduler.ScheduleRecurring(jobId, cronExpression, new {JobName}Command())` a `RegisterRecurringJobsStartupTask.cs`

### Novo Module DbContext
- [ ] Adicionar `await MigrateDatabaseAsync<{Module}DbContext>(scope, stoppingToken)` a `MigrateDatabaseStartupTask.cs`

### Novo DTO de requisição
- [ ] Criar `src/Web/Endpoints/Modules/{Module}/Requests/Commands/{Operation}Request.cs`
- [ ] `sealed class` com propriedades públicas e valores default
- [ ] Usar `[FromRoute(Name = Routes.ConstantName)]` para segmentos de URL — nunca hardcode a string
- [ ] Usar `[FromBody]` implicitamente para campos de corpo POST/PUT — nenhum atributo necessário
- [ ] Usar `[FromQuery]` para parâmetros de query string

### Novo Endpoint
- [ ] Criar `src/Web/Endpoints/Modules/{Module}/{Operation}Endpoint.cs`
- [ ] Herda de `EndpointBaseAsync.WithRequest<TRequest>.WithActionResult` ou `.WithActionResult<TResponse>`
- [ ] `[ApiVersion("1.0")]` — obrigatório
- [ ] `[Http{Verb}(Routes.Constant)]` — use constante de rota, nunca hardcode
- [ ] `[ProducesResponseType]` — um por possível status code HTTP
- [ ] `[SwaggerOperation(Summary, Description, Tags)]` — obrigatório
- [ ] Pipeline: `Result.Create(request).Map(...).Bind(...).Match(onSuccess, this.HandleFailure)`
- [ ] Sem corpo de resposta: `.Match(() => Ok(), this.HandleFailure)`
- [ ] Com corpo de resposta: `.Match(Ok, this.HandleFailure)`

---

## O Que Exige Registro Manual

- Projection handlers → `ServiceCollectionExtensions.AddEventInteractors()`
- Endpoints consumer MassTransit → `ConsumerConfiguration.cs`
- Serviços genéricos abertos → `PersistenceServiceInstaller`
- `IIdentityApplicationService` → `ApplicationServiceInstaller` via `TryAddTransient`

## O Que É Descoberto Automaticamente (sem registro necessário)

- Handlers MediatR → escaneados a partir de `Application.AssemblyReference.Assembly`
- Validators FluentValidation → escaneados a partir de `Application.AssemblyReference.Assembly`
- Services `ITransient` / `IScoped` → scan do Scrutor em `IdentityModuleInstaller`
- Consumers MassTransit → `AddConsumers(AssemblyReference.Assembly)` em `EventBusServiceInstaller`