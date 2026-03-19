using Core.Domain.Primitives.Interfaces;

namespace Identity.Domain.Projection;

public static class ProjectionModel
{
    public record User(
        Guid Id,
        string Name,
        string Email,
        string Role,
        string Status,
        Guid? AccountancyId,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt,
        bool IsDeleted = false
    ) : IProjectionModel
    { }
}