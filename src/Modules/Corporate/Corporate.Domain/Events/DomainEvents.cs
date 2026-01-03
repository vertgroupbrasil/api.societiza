using Core.Domain.Entities.Interfaces;

namespace Corporate.Domain.Events;

public static class DomainEvents
{
    public sealed record AccountancyCreated(
        Guid Id,
        string TenantId
    ) : IDomainEvent;
}