using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Payment.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment.Domain.Entities.Payment>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.SubscriptionId).IsRequired();

        builder.Property(e => e.Amount).HasColumnType("decimal(18,2)").HasPrecision(18, 2).IsRequired();
        builder.Property(e => e.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("BRL");
        builder.Property(e => e.Description).IsRequired().HasMaxLength(500);

        builder.Property(e => e.Status).IsRequired().HasConversion<string>();
        builder.Property(e => e.Method).HasConversion<string?>();

        builder.Property(e => e.AbacatePayCheckoutId).HasMaxLength(255);
        builder.Property(e => e.AbacatePayPaymentId).HasMaxLength(255);

        builder.HasIndex(e => e.SubscriptionId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.AbacatePayCheckoutId).IsUnique();
        builder.HasIndex(e => e.PaidAt);
        builder.HasIndex(e => new { e.SubscriptionId, e.Status });

        builder.HasOne<Subscription.Domain.Entities.Subscription>()
            .WithMany()
            .HasForeignKey(e => e.SubscriptionId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}