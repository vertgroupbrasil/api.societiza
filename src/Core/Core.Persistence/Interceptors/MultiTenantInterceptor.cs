using Core.Domain.Entities;
using Core.Domain.Entities.Interfaces;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Core.Persistence.Interceptors;

public sealed class MultiTenantInterceptor : SaveChangesInterceptor
{
    private readonly IMultiTenantContextAccessor<Tenant>? _multiTenantContextAccessor;

    public MultiTenantInterceptor(IMultiTenantContextAccessor<Tenant>? multiTenantContextAccessor = null)
    {
        _multiTenantContextAccessor = multiTenantContextAccessor;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        SetTenantId(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        SetTenantId(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    private void SetTenantId(DbContext? context)
    {
        if (context == null) 
            return;

        var tenantId = _multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id;

        if (string.IsNullOrWhiteSpace(tenantId))
            return;

        var entries = context.ChangeTracker
            .Entries<ITenantEntity>()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in entries)
        {
            var entity = entry.Entity;

            if (string.IsNullOrWhiteSpace(entity.TenantId))
            {
                var property = entry.Property(nameof(ITenantEntity.TenantId));
                property.CurrentValue = tenantId;
            }
        }

        var modifiedEntries = context.ChangeTracker
            .Entries<ITenantEntity>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in modifiedEntries)
        {
            var tenantIdProperty = entry.Property(nameof(ITenantEntity.TenantId));

            if (tenantIdProperty.IsModified)
            {
                tenantIdProperty.IsModified = false;
                tenantIdProperty.CurrentValue = tenantIdProperty.OriginalValue;
            }
        }
    }

}