using Core.Domain.Events.Interfaces;
using Core.Domain.Primitives;

namespace Identity.Domain.Aggregates;

public class User : AggregateRoot
{
    protected override void ApplyEvent(IDomainEvent @event)
        => throw new NotImplementedException();
}