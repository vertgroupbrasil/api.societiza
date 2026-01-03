using Corporate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corporate.Persistence.Configurations;

public class AccountancyConfiguration : IEntityTypeConfiguration<Accountancy>
{
    public void Configure(EntityTypeBuilder<Accountancy> builder)
    {
        builder.ToTable("Accountancies");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.LegalName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.TradeName).HasMaxLength(200);

        builder.Property(e => e.CNPJ).IsRequired().HasMaxLength(14);
        builder.Property(e => e.Address).IsRequired().HasMaxLength(500);
        builder.Property(e => e.City).IsRequired().HasMaxLength(100);
        builder.Property(e => e.State).IsRequired().HasMaxLength(2);
        builder.Property(e => e.PostalCode).IsRequired().HasMaxLength(8);

        builder.Property(e => e.Phone).IsRequired().HasMaxLength(11);
        builder.Property(e => e.Email).IsRequired().HasMaxLength(200);

        builder.Property(e => e.Notes).HasMaxLength(1000);
        builder.Property(e => e.TenantId).IsRequired().HasMaxLength(50);

        builder.HasIndex(e => e.CNPJ).IsUnique();

        builder.HasOne(a => a.Tenant)
            .WithMany()
            .HasForeignKey(a => a.TenantId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}