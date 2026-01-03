using Core.Domain.Entities.Interfaces;

namespace Core.Domain.Entities;

public class Global : IAuditableEntity, ISoftDeletable
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid CreatedById { get; set; }
    public Guid? UpdatedById { get; set; }

    public bool IsDeleted { get; protected set; } = false;
    public DateTimeOffset? DeletedAt { get; protected set; }
    public Guid? DeletedById { get; protected set; }

    public override bool Equals(object? obj)
        => obj is Global global && Id.Equals(global.Id);

    public override int GetHashCode()
        => HashCode.Combine(Id);

    public void MarkAsDeleted()
    {
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        // DeletedById = deletedById;
    }
}