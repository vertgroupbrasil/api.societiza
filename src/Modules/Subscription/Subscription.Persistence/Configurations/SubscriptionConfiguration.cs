using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Subscription.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription.Domain.Entities.Subscription>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Subscription> builder)
    {
        builder.ToTable("Subscriptions");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.PlanId).IsRequired();

        builder.Property(e => e.Status).IsRequired().HasConversion<string>();
        builder.Property(e => e.BillingCycle).IsRequired().HasConversion<string>();
        builder.Property(e => e.MonthlyAmount).HasPrecision(18, 2);
        builder.Property(e => e.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("BRL");

        builder.Property(e => e.AbacatePaySubscriptionId).HasMaxLength(255);
        builder.Property(e => e.TenantId).IsRequired().HasMaxLength(50);

        builder.HasIndex(e => e.TenantId).IsUnique();
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.NextBillingDate);
        builder.HasIndex(e => e.AbacatePaySubscriptionId).IsUnique();

        builder.HasOne(a => a.Tenant)
            .WithMany()
            .HasForeignKey(s => s.TenantId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Plan)
            .WithMany()
            .HasForeignKey(e => e.PlanId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}