# Camada: Persistence

> Responsável por infraestrutura pura de dados — sem lógica de negócio.
> Contém DbContext EF Core, context MongoDB, configurações EF, migrations e a implementação do repositório de projection.
> Nunca referencia a camada Application.

---

## Fronteira de Responsabilidade

- **Persistence** é dona de: DbContext, MongoDbContext, configurações EF, migrations, `IIdentityProjection<T>`, `IdentityProjection<T>`
- **Persistence** NÃO é dona de: lógica de negócio, orquestração de serviços, consumers MassTransit, serviços JWT/password

---

## Event Store — PostgreSQL

### Como AppendEventsAsync Funciona

Definido em `src/Core/Core.Application/Services/ApplicationService.cs`:

```csharp
public async Task AppendEventsAsync<TAggregate>(
    TAggregate aggregate, CancellationToken cancellationToken)
    where TAggregate : IAggregateRoot
{
    var eventsToPublish = new List<IEvent>();

    await unitOfWork.ExecuteAsync(async ct =>
    {
        while (aggregate.TryDequeueEvent(out var @event))
        {
            var storeEvent = StoreEvent<TAggregate>.Create(aggregate, @event);
            await eventStore.AppendAsync(storeEvent, ct);

            if (storeEvent.Version % 10 is 0)           // automatic snapshot every 10 events
            {
                var snapshot = Snapshot<TAggregate>.Create(aggregate, storeEvent);
                await eventStore.AppendAsync(snapshot, ct);
            }

            eventsToPublish.Add(@event);
        }
    }, cancellationToken);

    // Published AFTER commit — guarantees RabbitMQ publish only if PostgreSQL succeeded
    foreach (var @event in eventsToPublish)
        await eventBusGateway.PublishAsync(@event, cancellationToken);
}
```

Duas fases distintas:
1. **Dentro da transação** (`UnitOfWork.ExecuteAsync`): drena a fila de eventos do aggregate, persiste cada `StoreEvent` no PostgreSQL, gera snapshot a cada 10 eventos
2. **Fora da transação**: publica eventos no RabbitMQ via `IEventBus` — somente após o commit

### Como LoadAggregateAsync Funciona

Definido em `src/Core/Core.Application/Services/ApplicationService.cs`:

```csharp
public async Task<Result<TAggregate>> LoadAggregateAsync<TAggregate>(
    Guid id, CancellationToken cancellationToken)
    where TAggregate : class, IAggregateRoot, new()
{
    var snapshot = await eventStore.GetSnapshotAsync<TAggregate>(id, cancellationToken);
    var events = await eventStore.GetStreamAsync<TAggregate>(
        id, snapshot?.Version ?? 0, cancellationToken);

    if (snapshot is null && events is { Count: 0 })
        return Result.Failure<TAggregate>(new NotFoundError(...));

    var aggregate = snapshot?.Aggregate ?? new();
    aggregate.LoadFromStream(events);   // replays events via ApplyEvent() → When()

    if (aggregate.IsDeleted)
        return Result.Failure<TAggregate>(new ConflictError(...));

    return Result.Success(aggregate);
}
```

Fluxo: busca o snapshot mais recente → carrega apenas eventos **após** o snapshot (`version > snapshot.Version`) → reconstitui o aggregate reproduzindo eventos via `LoadFromStream` → retorna `Result<TAggregate>`.

### Camada SQL do EventStore

Definido em `src/Core/Core.Persistence/EventStore/EventStore.cs`:

```csharp
public async Task AppendAsync<TAggregate>(StoreEvent<TAggregate> storeEvent, CancellationToken ct)
    where TAggregate : IAggregateRoot
{
    await dbContext.Set<StoreEvent<TAggregate>>().AddAsync(storeEvent, ct);
    await dbContext.SaveChangesAsync(ct);
}

public Task<List<IDomainEvent>> GetStreamAsync<TAggregate>(Guid id, ulong version, CancellationToken ct)
    where TAggregate : IAggregateRoot
    => dbContext.Set<StoreEvent<TAggregate>>()
        .AsNoTracking()
        .Where(@event => @event.AggregateId.Equals(id))
        .Where(@event => @event.Version > version)   // only events after the snapshot
        .Select(@event => @event.Event)
        .ToListAsync(ct);
```

`IDomainEvent` é serializado/desserializado como JSON com `TypeNameHandling.Auto` — obrigatório para preservar o tipo concreto ao desserializar a interface.

### Tabelas Geradas (por aggregate)

Convenção: uma tabela `StoreEvents` e uma tabela `Snapshots` por aggregate.

```sql
-- src/Modules/Identity/Identity.Persistence/Migrations/
CREATE TABLE identity."UserInviteStoreEvents" (
    "AggregateId"  uuid,
    "Version"      numeric(20,0),   -- ulong
    "EventType"    varchar(50),
    "Event"        text,             -- JSON with TypeNameHandling
    "Timestamp"    timestamptz,
    PRIMARY KEY ("Version", "AggregateId")
);

CREATE TABLE identity."UserInviteSnapshots" (
    "AggregateId"  uuid,
    "Version"      numeric(20,0),
    "Aggregate"    text,             -- full aggregate JSON
    "Timestamp"    timestamptz,
    PRIMARY KEY ("Version", "AggregateId")
);
```

### Configurações EF

Definidas em `src/Modules/Identity/Identity.Persistence/Configurations/`:

```csharp
// StoreEventConfiguration.cs — empty class, inherits all config from Core
public class UserInviteStoreEventConfiguration : StoreEventConfiguration<UserInvite>;

// SnapshotConfiguration.cs — empty class, inherits all config from Core
public class UserInviteSnapshotConfiguration : SnapshotConfiguration<UserInvite>;
```

Ambas são classes vazias — toda configuração EF vive nas classes base genéricas em Core. Convenção de nome de tabela: `{AggregateName}StoreEvents` e `{AggregateName}Snapshots`.

**Todo novo aggregate exige duas classes de configuração — uma para StoreEvents, uma para Snapshots.**

---

## MongoDB Projections

### Hierarquia

```
IProjection<T>                          (Core.Domain)
  └── IIdentityProjection<T>            (Identity.Persistence)

Projection<T>                           (Core.Persistence) ← real implementation
  └── IdentityProjection<T>             (Identity.Persistence)

IMongoDbContext                         (Core.Persistence)
  └── IIdentityProjectionDbContext      (Identity.Persistence)
      └── IdentityProjectionDbContext   (Identity.Persistence)
```

### IdentityProjection

Definido em `src/Modules/Identity/Identity.Persistence/Projection/`:

```csharp
// IIdentityProjection<T>.cs — marker interface, enables typed injection per module
public interface IIdentityProjection<T> : IProjection<T>
    where T : IProjectionModel;

// IdentityProjection<T>.cs — passes the correct DbContext to the Core implementation
public class IdentityProjection<T>(IIdentityProjectionDbContext context)
    : Projection<T>(context), IIdentityProjection<T>
    where T : IProjectionModel;
```

### Implementação de Projection em Core

Definida em `src/Core/Core.Persistence/Projection/Projection.cs`:

```csharp
public class Projection<TProjection>(IMongoDbContext context) : IProjection<TProjection>
    where TProjection : IProjectionModel
{
    private readonly IMongoCollection<TProjection> _collection
        = context.GetCollection<TProjection>();  // collection name = typeof(T).Name

    public Task<TProjection?> FindAsync(
        Expression<Func<TProjection, bool>> predicate, CancellationToken cancellationToken)
        => _collection.AsQueryable()
                      .Where(predicate)
                      .FirstOrDefaultAsync(cancellationToken);

    // Upsert: replaces the full document if exists (Id match), inserts if not
    public ValueTask ReplaceInsertAsync(TProjection replacement, CancellationToken cancellationToken)
        => OnReplaceAsync(
            replacement,
            projection => projection.Id == replacement.Id,
            cancellationToken);
}
```

`ReplaceInsertAsync` é o método usado em todos os Projection Handlers. É um upsert de documento completo — por isso handlers usam `existing with { Field = newValue }` para produzir um novo record com todos os campos preenchidos antes de passá-lo ao upsert.

### Convenção de Nome de Collection MongoDB

O nome da collection é sempre `typeof(TProjection).Name` — nenhum registro explícito é necessário.

- `ProjectionModel.User` → collection `User`
- `ProjectionModel.UserInvite` → collection `UserInvite`

### Base MongoDbContext

Definida em `src/Core/Core.Persistence/Projection/Abstractions/MongoDbContext.cs`:

```csharp
public abstract class MongoDbContext : IMongoDbContext
{
    protected MongoDbContext(string connectionString)
    {
        MongoUrl mongoUrl = new(connectionString);
        MongoClientSettings settings = MongoClientSettings.FromUrl(mongoUrl);
        settings.SslSettings = new SslSettings { EnabledSslProtocols = SslProtocols.Tls13 };
        _mongoClient = new MongoClient(settings);
        _mongoDatabase = _mongoClient.GetDatabase(mongoUrl.DatabaseName);
    }

    public IMongoCollection<T> GetCollection<T>()
        => _mongoDatabase.GetCollection<T>(typeof(T).Name);
}
```

---

## DbContext

### IdentityDbContext

Definido em `src/Modules/Identity/Identity.Persistence/Context/IdentityDbContext.cs`:

- Aplica automaticamente todas as configurações EF do assembly Persistence
- Usa o schema `identity` (definido em `Constants/Schemas.cs`)
- Registrado em `PersistenceServiceInstaller` com Npgsql e tabela de histórico de migrations no schema `identity`

### IdentityProjectionDbContext

Definido em `src/Modules/Identity/Identity.Persistence/Context/IdentityProjectionDbContext.cs`:

- Herda `MongoDbContext` de Core
- Implementa `IIdentityProjectionDbContext` (interface marker para DI tipada)
- Connection string lida da seção de configuração `Projections:Identity`

---

## O que NUNCA fazer em Persistence

**Event Store**
- Nunca chame `AppendEventsAsync` dentro de uma transação aberta manualmente — ele gerencia seu próprio `UnitOfWork`.
- Nunca carregue um aggregate via query SQL ou projection — sempre use `LoadAggregateAsync`.
- Nunca pule as classes de configuração EF para um novo aggregate. Todo aggregate precisa de `StoreEventConfiguration<T>` e `SnapshotConfiguration<T>`.

**Projections**
- Nunca escreva diretamente em uma collection MongoDB — sempre passe por `IIdentityProjection<T>`.
- Nunca use `InsertOneAsync` para projections — sempre use `ReplaceInsertAsync` (upsert).
- Nunca registre explicitamente uma nova collection de projection — o nome da collection é inferido de `typeof(T).Name`.

**Fronteiras de camada**
- Nunca referencie a camada Application a partir de Persistence.
- Nunca coloque lógica de negócio ou orquestração de serviços em classes de Persistence.