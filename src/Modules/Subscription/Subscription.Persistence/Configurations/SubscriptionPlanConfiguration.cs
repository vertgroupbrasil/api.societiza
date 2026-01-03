using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subscription.Domain.Entities;
using Subscription.Persistence.Seeding;

namespace Subscription.Persistence.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("SubscriptionPlans");

        builder.HasKey(e => e.Id);

        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.Property(e => e.MonthlyPrice).HasPrecision(18, 2);
        builder.Property(e => e.YearlyPrice).HasPrecision(18, 2);

        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => e.IsActive);

        builder.HasMany(e => e.Features)
           .WithOne(f => f.Plan)
           .HasForeignKey(f => f.PlanId)
           .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(SubscriptionPlan.Features))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasData(
            new {
                Id = SubscriptionSeedIds.PlanTrial,
                Code = "trial",
                Name = "Trial",
                Description = "Trial plan (7-15 days)",
                MonthlyPrice = 0m,
                YearlyPrice = 0m,
                IsActive = true,
                IsPublic = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new {
                Id = SubscriptionSeedIds.PlanEssential,
                Code = "essential",
                Name = "Essential",
                Description = "Essential plan",
                MonthlyPrice = 299.90m,
                YearlyPrice = 3598.80m,
                IsActive = true,
                IsPublic = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new
            {
                Id = SubscriptionSeedIds.PlanProfessional,
                Code = "professional",
                Name = "Professional",
                Description = "Professional plan",
                MonthlyPrice = 791.65m,
                YearlyPrice = 9499.90m,
                IsActive = true,
                IsPublic = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new
            {
                Id = SubscriptionSeedIds.PlanEnterprise,
                Code = "enterprise",
                Name = "Enterprise",
                Description = "Enterprise plan",
                MonthlyPrice = 1299.90m,
                YearlyPrice = 15598.80m,
                IsActive = true,
                IsPublic = true,
                CreatedAt = DateTimeOffset.UtcNow
            });
    }
}