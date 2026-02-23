using Core.Domain.Events;
using Core.Domain.Events.Interfaces;

namespace Accountancy.Domain.Events;

public static class DomainEvents
{
    public sealed record AccountancyCreated(
        Guid Id,
        string LegalName,
        string CNPJ,
        string? TradeName,
        string Address,
        string City,
        string State,
        string PostalCode,
        string Phone,
        string? Email,
        DateTimeOffset CreatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record AccountancyUpdated(
        Guid Id,
        string LegalName,
        string CNPJ,
        string? TradeName,
        string Address,
        string City,
        string State,
        string PostalCode,
        string Phone,
        string? Email,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record AccountancyDeleted(
        Guid Id,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;
}