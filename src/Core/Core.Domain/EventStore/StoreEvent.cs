using Core.Domain.Events.Interfaces;
using Core.Domain.Primitives.Interfaces;

namespace Core.Domain.EventStore;

public record StoreEvent<TAggregate>(Guid AggregateId, string EventType, IDomainEvent Event, ulong Version, DateTimeOffset Timestamp)
    where TAggregate : IAggregateRoot
{
    // Persist the version carried by the event itself.
    // Multi-event aggregates depend on this to keep event ordering consistent.
    public static StoreEvent<TAggregate> Create(TAggregate aggregate, IDomainEvent @event)
        => new(aggregate.Id, @event.GetType().Name, @event, @event.Version, @event.Timestamp);
}
