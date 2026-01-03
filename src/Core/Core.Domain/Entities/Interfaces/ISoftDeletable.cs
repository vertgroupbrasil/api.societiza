namespace Core.Domain.Entities.Interfaces;

public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTimeOffset? DeletedAt { get; }
    Guid? DeletedById { get; }
}