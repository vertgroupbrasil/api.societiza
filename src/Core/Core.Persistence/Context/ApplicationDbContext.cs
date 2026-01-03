using Core.Domain.Entities;
using Core.Domain.Entities.Interfaces;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Finbuckle.MultiTenant.EntityFrameworkCore.Stores.EFCoreStore;
using Microsoft.EntityFrameworkCore;

namespace Core.Persistence.Context;

public class ApplicationDbContext : EFCoreStoreDbContext<Tenant>
{
    private readonly IMultiTenantContextAccessor<Tenant>? _multiTenantContextAccessor;

    // Constructor for use at runtime
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IMultiTenantContextAccessor<Tenant> multiTenantContextAccessor)
        : base(options)
    {
        _multiTenantContextAccessor = multiTenantContextAccessor;
    }

    // Constructor for use at design-time (migrations)
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    { 
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ApplySoftDeleteFilter(modelBuilder);

        modelBuilder.ConfigureMultiTenant();
        ApplyConfigurationsFromModules(modelBuilder);
    }

    private static void ApplyConfigurationsFromModules(ModelBuilder modelBuilder)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        var moduleAssemblies = assemblies
            .Where(a =>
            {
                var name = a.GetName().Name;
                return name != null &&
                       !name.StartsWith("System") &&
                       !name.StartsWith("Microsoft") &&
                       name.EndsWith(".Persistence");
            })
            .ToList();

        foreach (var assembly in moduleAssemblies)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }
    }

    private static void ApplySoftDeleteFilter(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(ApplicationDbContext)
                    .GetMethod(
                        nameof(SetSoftDeleteFilter),
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    ?.MakeGenericMethod(entityType.ClrType);

                method?.Invoke(null, new object[] { modelBuilder });
            }
        }
    }

    private static void SetSoftDeleteFilter<T>(ModelBuilder modelBuilder)
        where T : class, ISoftDeletable
    {
        modelBuilder
            .Entity<T>()
            .HasQueryFilter(e => !e.IsDeleted);
    }

    public override int SaveChanges()
    {
        UpdateAuditableEntities();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditableEntities()
    {
        var entries = ChangeTracker.Entries<IAuditableEntity>();
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(IAuditableEntity.CreatedAt)).IsModified = false;
                entry.Property(nameof(IAuditableEntity.CreatedById)).IsModified = false;
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}