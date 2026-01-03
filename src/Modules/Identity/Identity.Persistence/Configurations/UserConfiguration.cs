using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Email).IsRequired().HasMaxLength(200);
        builder.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.LastName).IsRequired().HasMaxLength(100);

        builder.Property(e => e.Role).IsRequired().HasConversion<int>();

        builder.Property(e => e.AuthProvider).HasConversion<int>();
        builder.Property(e => e.OAuthProviderId).HasMaxLength(256);

        builder.Property(e => e.TenantId).IsRequired().HasMaxLength(50);
        builder.Property(e => e.PasswordHash).IsRequired().HasMaxLength(100);

        builder.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
    }
}