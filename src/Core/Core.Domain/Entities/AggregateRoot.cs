using Core.Domain.Entities.Interfaces;
using Core.Domain.Exceptions;

namespace Core.Domain.Entities;

public abstract class AggregateRoot : Entity
{
    private readonly Queue<IDomainEvent> _domainEvents = new();

    public bool TryDequeueDomainEvent(out IDomainEvent domainEvent)
        => _domainEvents.TryDequeue(out domainEvent!);

    protected void AddDomainEvent(IDomainEvent @event)
    {
        if (IsDeleted)
            throw new AggregateIsDeletedException(Id);

        _domainEvents.Enqueue(@event);
    }
}