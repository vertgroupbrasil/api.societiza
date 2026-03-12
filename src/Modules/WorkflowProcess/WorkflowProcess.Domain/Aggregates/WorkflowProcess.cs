using Core.Domain.Events.Interfaces;
using Core.Domain.Primitives;

namespace WorkflowProcess.Domain.Aggregates;

public class WorkflowProcess : AggregateRoot
{
    protected override void ApplyEvent(IDomainEvent @event)
        => throw new NotImplementedException();
}