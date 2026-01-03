using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Identifier).IsRequired().HasMaxLength(23);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);

        builder.Property(e => e.DatabaseProvider).HasConversion<int>();
        builder.Property(e => e.ConnectionString).HasMaxLength(1024);

        builder.HasIndex(e => e.Identifier).IsUnique();
    }
}