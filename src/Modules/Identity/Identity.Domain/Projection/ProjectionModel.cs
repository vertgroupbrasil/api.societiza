using Core.Domain.Primitives.Interfaces;

namespace Identity.Domain.Projection;

public static class ProjectionModel
{
    public record User(
        Guid Id,
        string FirstName,
        string LastName,
        string Email,
        string Role,
        Guid? AccountancyId,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt,
        Guid? InvitationLinkId = null,
        bool IsDeleted = false
    ) : IProjectionModel;

    public record InvitationLink(
        Guid Id,
        string TargetRole,
        string Status,
        Guid? AccountancyId,
        Guid CreatedByUserId,
        string TokenHash,
        int MaxUses,
        int Uses,
        DateTimeOffset ExpiresAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt,
        Guid? RevokedByUserId = null,
        bool IsDeleted = false
    ) : IProjectionModel;

    public record RefreshToken(
        Guid Id,
        Guid UserId,
        string TokenHash,
        DateTimeOffset ExpiresAt,
        bool IsRevoked,
        DateTimeOffset? RevokedAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt,
        bool IsDeleted = false
    ) : IProjectionModel;

    public record PasswordResetToken(
        Guid Id,
        Guid UserId,
        string TokenHash,
        DateTimeOffset ExpiresAt,
        bool IsUsed,
        DateTimeOffset? UsedAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt,
        bool IsDeleted = false
    ) : IProjectionModel;
}