using Core.Domain.Entities.Interfaces;

namespace Core.Domain.Entities;

public class Entity : ITenantEntity, IAuditableEntity, ISoftDeletable
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public string TenantId { get; protected set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid CreatedById { get; set; }
    public Guid? UpdatedById { get; set; }

    public bool IsDeleted { get; protected set; } = false;
    public DateTimeOffset? DeletedAt { get; protected set; }
    public Guid? DeletedById { get; protected set; }

    public override bool Equals(object? obj)
        => obj is Entity entity && Id.Equals(entity.Id);

    public override int GetHashCode()
        => HashCode.Combine(Id);

    public void SetTenantId(string tenantId) => 
        TenantId = tenantId;

    public void MarkAsDeleted()
    {
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        // DeletedById = deletedById;
    }
}