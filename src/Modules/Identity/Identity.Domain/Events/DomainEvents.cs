using Core.Domain.Events;
using Core.Domain.Events.Interfaces;
using Identity.Domain.Enumerations;

namespace Identity.Domain.Events;

public static class DomainEvents
{
    public sealed record UserCreated(
        Guid Id,
        string Name,
        string Email,
        string PasswordHash,
        UserRole Role,
        Guid? AccountancyId,
        DateTimeOffset CreatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record UserInvited(
        Guid Id,
        string Name,
        string Email,
        string PasswordHash,
        UserRole Role,
        Guid AccountancyId,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record UserUpdated(
        Guid Id,
        string Name,
        string Email,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record UserPasswordChanged(
        Guid Id,
        string PasswordHash,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record UserRoleChanged(
        Guid Id,
        UserRole Role,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record UserActivated(
        Guid Id,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record UserDeactivated(
        Guid Id,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record UserDeleted(
        Guid Id,
        ulong Version
    ) : Message, IDomainEvent;
}