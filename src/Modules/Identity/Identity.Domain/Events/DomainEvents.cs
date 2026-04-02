using Core.Domain.Events;
using Core.Domain.Events.Interfaces;

namespace Identity.Domain.Events;

public static class DomainEvents
{
    #region UserInvite Events
    public sealed record UserInviteCreated(
        Guid Id,
        string Email,
        string Role,
        Guid? AccountancyId,
        string TokenHash,
        string Status,
        DateTimeOffset ExpiresAt,
        DateTimeOffset CreatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record UserInviteResent(
        Guid Id,
        string TokenHash,
        DateTimeOffset ExpiresAt,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record UserInviteAccepted(
        Guid Id,
        string Status,
        DateTimeOffset UsedAt,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record UserInviteRevoked(
        Guid Id,
        string Status,
        DateTimeOffset UsedAt,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record UserInviteExpired(
        Guid Id,
        string Status,
        DateTimeOffset UsedAt,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;
    #endregion

    #region User Events
    public sealed record UserRegistrationCompleted(
        Guid Id,
        Guid InviteId,
        string FirstName,
        string LastName,
        string Email,
        string PasswordHash,
        string Role,
        Guid? AccountancyId,
        DateTimeOffset CreatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record UserRefreshTokenAdded(
        Guid UserId,
        Guid TokenId,
        string TokenHash,
        DateTimeOffset ExpiresAt,
        DateTimeOffset CreatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record UserRefreshTokenRevoked(
        Guid UserId,
        Guid TokenId,
        DateTimeOffset RevokedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record UserPasswordResetTokenAdded(
        Guid UserId,
        Guid TokenId,
        string TokenHash,
        DateTimeOffset ExpiresAt,
        DateTimeOffset CreatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record UserPasswordResetTokenUsed(
        Guid UserId,
        Guid TokenId,
        DateTimeOffset UsedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record UserPasswordUpdated(
        Guid Id,
        string PasswordHash,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;
    #endregion
}