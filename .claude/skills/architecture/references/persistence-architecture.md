# Layer: Persistence

> Responsible for pure data infrastructure — no business logic.
> Contains EF Core DbContext, MongoDB context, EF configurations, migrations, and the projection repository implementation.
> Never references the Application layer.

---

## Responsibility Boundary

- **Persistence** owns: DbContext, MongoDbContext, EF configurations, migrations, `IIdentityProjection<T>`, `IdentityProjection<T>`
- **Persistence** does NOT own: business logic, service orchestration, MassTransit consumers, JWT/password services

---

## Event Store — PostgreSQL

### How AppendEventsAsync Works

Defined in `src/Core/Core.Application/Services/ApplicationService.cs`:

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

Two distinct phases:
1. **Inside the transaction** (`UnitOfWork.ExecuteAsync`): drains the aggregate event queue, persists each `StoreEvent` to PostgreSQL, generates a snapshot every 10 events
2. **Outside the transaction**: publishes events to RabbitMQ via `IEventBus` — only after commit

### How LoadAggregateAsync Works

Defined in `src/Core/Core.Application/Services/ApplicationService.cs`:

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

Flow: fetches the latest snapshot → loads only events **after** the snapshot (`version > snapshot.Version`) → reconstitutes the aggregate by replaying events via `LoadFromStream` → returns `Result<TAggregate>`.

### EventStore SQL Layer

Defined in `src/Core/Core.Persistence/EventStore/EventStore.cs`:

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

`IDomainEvent` is serialized/deserialized as JSON with `TypeNameHandling.Auto` — required to preserve the concrete type when deserializing the interface.

### Generated Tables (per aggregate)

Convention: one `StoreEvents` table and one `Snapshots` table per aggregate.

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

### EF Configurations

Defined in `src/Modules/Identity/Identity.Persistence/Configurations/`:

```csharp
// StoreEventConfiguration.cs — empty class, inherits all config from Core
public class UserInviteStoreEventConfiguration : StoreEventConfiguration<UserInvite>;

// SnapshotConfiguration.cs — empty class, inherits all config from Core
public class UserInviteSnapshotConfiguration : SnapshotConfiguration<UserInvite>;
```

Both are empty classes — all EF configuration lives in the generic base classes in Core. Table name convention: `{AggregateName}StoreEvents` and `{AggregateName}Snapshots`.

**Every new aggregate requires two configuration classes — one for StoreEvents, one for Snapshots.**

---

## MongoDB Projections

### Hierarchy

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

Defined in `src/Modules/Identity/Identity.Persistence/Projection/`:

```csharp
// IIdentityProjection<T>.cs — marker interface, enables typed injection per module
public interface IIdentityProjection<T> : IProjection<T>
    where T : IProjectionModel;

// IdentityProjection<T>.cs — passes the correct DbContext to the Core implementation
public class IdentityProjection<T>(IIdentityProjectionDbContext context)
    : Projection<T>(context), IIdentityProjection<T>
    where T : IProjectionModel;
```

### Core Projection Implementation

Defined in `src/Core/Core.Persistence/Projection/Projection.cs`:

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

`ReplaceInsertAsync` is the method used in all Projection Handlers. It is a full document upsert — this is why handlers use `existing with { Field = newValue }` to produce a new record with all fields populated before passing it to the upsert.

### MongoDB Collection Name Convention

The collection name is always `typeof(TProjection).Name` — no explicit registration needed.

- `ProjectionModel.User` → collection `User`
- `ProjectionModel.UserInvite` → collection `UserInvite`

### MongoDbContext Base

Defined in `src/Core/Core.Persistence/Projection/Abstractions/MongoDbContext.cs`:

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

Defined in `src/Modules/Identity/Identity.Persistence/Context/IdentityDbContext.cs`:

- Applies all EF configurations from the Persistence assembly automatically
- Uses schema `identity` (defined in `Constants/Schemas.cs`)
- Registered in `PersistenceServiceInstaller` with Npgsql and migration history table in the `identity` schema

### IdentityProjectionDbContext

Defined in `src/Modules/Identity/Identity.Persistence/Context/IdentityProjectionDbContext.cs`:

- Inherits `MongoDbContext` from Core
- Implements `IIdentityProjectionDbContext` (marker interface for typed DI)
- Connection string read from configuration section `Projections:Identity`

---

## What to NEVER do in Persistence

**Event Store**
- Never call `AppendEventsAsync` inside a manually opened transaction — it manages its own `UnitOfWork`.
- Never load an aggregate via SQL query or projection — always use `LoadAggregateAsync`.
- Never skip the EF configuration classes for a new aggregate. Every aggregate needs `StoreEventConfiguration<T>` and `SnapshotConfiguration<T>`.

**Projections**
- Never write to a MongoDB collection directly — always go through `IIdentityProjection<T>`.
- Never use `InsertOneAsync` for projections — always use `ReplaceInsertAsync` (upsert).
- Never register a new projection collection explicitly — the collection name is inferred from `typeof(T).Name`.

**Layer boundaries**
- Never reference the Application layer from Persistence.
- Never place business logic or service orchestration in Persistence classes.