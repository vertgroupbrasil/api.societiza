using Core.Domain.EventStore;
using Core.Domain.Events;
using Core.Domain.Events.Interfaces;
using Core.Domain.Primitives;

namespace Web.Tests.Core.Domain.EventStore;

public class StoreEventTests
{
    [Fact]
    public void Create_UsesDomainEventVersion_InsteadOfAggregateVersion()
    {
        var aggregate = FakeAggregate.CreateWithVersion(7);
        var @event = new FakeDomainEvent(1);

        var storeEvent = StoreEvent<FakeAggregate>.Create(aggregate, @event);

        Assert.Equal((ulong)1, storeEvent.Version);
        Assert.Equal(aggregate.Id, storeEvent.AggregateId);
    }

    private sealed class FakeAggregate : AggregateRoot
    {
        public static FakeAggregate CreateWithVersion(ulong version)
        {
            var aggregate = new FakeAggregate();

            while (aggregate.Version < version)
                aggregate.RaiseFakeEvent();

            while (aggregate.TryDequeueEvent(out _))
            {
            }

            return aggregate;
        }

        private void RaiseFakeEvent()
            => RaiseEvent(eventVersion => new FakeDomainEvent(eventVersion));

        protected override void ApplyEvent(IDomainEvent @event)
        {
        }
    }

    private sealed record FakeDomainEvent(ulong Version) : Message, IDomainEvent;
}
