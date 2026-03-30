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
        bool IsDeleted = false
    ) : IProjectionModel;

    public record UserInvite(
        Guid Id,
        string Email,
        string Role,
        Guid? AccountancyId,
        string TokenHash,
        string Status,
        DateTimeOffset ExpiresAt,
        DateTimeOffset? UsedAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt,
        bool IsDeleted = false
    ) : IProjectionModel;
}