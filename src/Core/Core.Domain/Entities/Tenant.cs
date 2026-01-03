using Core.Domain.Entities.Interfaces;
using Core.Domain.Enumerations;
using Core.Shared.Extensions;
using Finbuckle.MultiTenant.Abstractions;

namespace Core.Domain.Entities;

public class Tenant : ITenantInfo, ISoftDeletable
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Identifier { get; set; } = null!;
    public string Name { get; set; } = null!;

    public DatabaseProvider? DatabaseProvider { get; private set; }
    public string? ConnectionString { get; private set; }
    public bool HasDedicatedDatabase => !string.IsNullOrWhiteSpace(ConnectionString);

    public DateTimeOffset EstablishedDate { get; private set; }
    public DateTimeOffset ExpirationDate { get; private set; }

    public bool IsDeleted { get; protected set; } = false;
    public DateTimeOffset? DeletedAt { get; protected set; }
    public Guid? DeletedById { get; protected set; }

    public static Tenant Create(
        string name,
        string cnpj,
        DatabaseProvider? databaseProvider,
        string? connectionString,
        DateTimeOffset? expirationDate)
    {
        var now = DateTimeOffset.UtcNow;
        
        var tenant = new Tenant
        {
            Id = Guid.NewGuid().ToString(),
            Identifier = $"tenant-{cnpj.OnlyDigits()}",
            Name = name.Trim(),
            DatabaseProvider = databaseProvider,
            ConnectionString = connectionString,
            EstablishedDate = now,
            ExpirationDate = expirationDate ?? now.AddYears(1)
        };

        return tenant;
    }

    public void Update(
        string name,
        string cnpj,
        DatabaseProvider? databaseProvider,
        string? connectionString)
    {
        Name = name.Trim();
        Identifier = $"tenant-{cnpj.OnlyDigits()}";
        DatabaseProvider = databaseProvider;
        ConnectionString = connectionString;
    }

    public void MarkAsDeleted()
    {
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        // DeletedById = deletedById;
    }

    public override bool Equals(object obj)
        => obj is Tenant info && Id.Equals(info.Id);

    public override int GetHashCode()
        => HashCode.Combine(Id);
}