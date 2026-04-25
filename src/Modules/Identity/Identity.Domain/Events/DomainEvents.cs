using Core.Domain.Events;
using Core.Domain.Events.Interfaces;

namespace Identity.Domain.Events;

public static class DomainEvents
{
    #region InvitationLink Events
    public sealed record InvitationLinkCreated(
        Guid Id,
        string TargetRole,
        string Status,
        Guid? AccountancyId,
        Guid CreatedByUserId,
        string Token,
        string TokenHash,
        int MaxUses,
        DateTimeOffset ExpiresAt,
        DateTimeOffset CreatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record InvitationLinkConsumed(
        Guid Id,
        int Uses,
        Guid ConsumedByUserId,
        DateTimeOffset ConsumedAt,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record InvitationLinkExhausted(
        Guid Id,
        string Status,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record InvitationLinkExpired(
        Guid Id,
        string Status,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;
    #endregion

    #region User Events
    public sealed record UserRegistrationCompleted(
        Guid Id,
        Guid InvitationLinkId,
        string FirstName,
        string LastName,
        string Email,
        string PasswordHash,
        string Role,
        Guid? AccountancyId,
        Guid SecurityStamp,
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

    public sealed record UserSecurityStampRegenerated(
        Guid UserId,
        Guid SecurityStamp,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record UserNameUpdated(
        Guid UserId,
        string FirstName,
        string LastName,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record UserPromotedToAccountancyAdmin(
        Guid UserId,
        Guid AccountancyId,
        string Role,
        Guid PromotedBy,
        DateTimeOffset PromotedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record UserDeleted(
        Guid UserId,
        Guid? AccountancyId,
        Guid DeletedBy,
        DateTimeOffset DeletedAt,
        ulong Version
    ) : Message, IDomainEvent;
    #endregion
}