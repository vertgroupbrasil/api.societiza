using Core.Domain.Events;
using Core.Domain.Events.Interfaces;

namespace Identity.Domain.Events;

public static class DomainEvents
{
    public sealed record UserInvited(
        Guid Id,
        string Name,
        string Email,
        string PasswordHash,
        string Role,
        string Status,
        Guid? AccountancyId,
        DateTimeOffset CreatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record UserRegistrationCompleted(
        Guid Id,
        string PasswordHash,
        string Status,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;
}
