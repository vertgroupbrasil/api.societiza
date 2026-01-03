using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subscription.Domain.Entities;
using Subscription.Domain.Enumerations;
using Subscription.Persistence.Seeding;

namespace Subscription.Persistence.Configurations;

public class PlanFeatureConfiguration : IEntityTypeConfiguration<PlanFeature>
{
    public void Configure(EntityTypeBuilder<PlanFeature> builder)
    {
        builder.ToTable("PlanFeatures");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.PlanId).IsRequired();

        builder.Property(e => e.FeatureKey).IsRequired().HasMaxLength(100);
        builder.Property(e => e.DisplayName).IsRequired().HasMaxLength(150);

        builder.Property(e => e.FeatureType).IsRequired().HasConversion<int>();
        builder.Property(e => e.FeatureScope).IsRequired().HasConversion<int>();

        builder.Property(e => e.LimitJson).HasColumnType("jsonb");

        builder.Property(e => e.IsUnlimited).IsRequired();

        builder.HasIndex(e => new { e.PlanId, e.FeatureKey }).IsUnique();
        builder.HasIndex(e => e.FeatureScope);

        builder.HasOne(f => f.Plan)
            .WithMany(p => p.Features)
            .HasForeignKey(f => f.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasData(
            // Trial plan features
            new
            {
                Id = SubscriptionSeedIds.FeatureTrialMaxUsers,
                PlanId = SubscriptionSeedIds.PlanTrial,
                FeatureKey = "max_users",
                DisplayName = "Maximum users",
                FeatureType = FeatureType.Integer,
                FeatureScope = FeatureScope.PerTenant,
                LimitNumber = 5L,
                IsUnlimited = false
            },
            new
            {
                Id = SubscriptionSeedIds.FeatureTrialMaxWorkflows,
                PlanId = SubscriptionSeedIds.PlanTrial,
                FeatureKey = "max_workflows",
                DisplayName = "Maximum workflows",
                FeatureType = FeatureType.Integer,
                FeatureScope = FeatureScope.PerTenant,
                LimitNumber = 20L,
                IsUnlimited = false
            },
            new
            {
                Id = SubscriptionSeedIds.FeatureTrialMaxStorage,
                PlanId = SubscriptionSeedIds.PlanTrial,
                FeatureKey = "max_storage",
                DisplayName = "Maximum storage (bytes)",
                FeatureType = FeatureType.Bytes,
                FeatureScope = FeatureScope.PerTenant,
                LimitNumber = 2_147_483_648L,
                IsUnlimited = false
            },
            new {
                Id = SubscriptionSeedIds.FeatureTrialWorkflowEnabled,
                PlanId = SubscriptionSeedIds.PlanTrial,
                FeatureKey = "workflow_enabled",
                DisplayName = "Workflow module enabled",
                FeatureType = FeatureType.Boolean,
                FeatureScope = FeatureScope.PerTenant,
                LimitBoolean = true,
                IsUnlimited = false
            },
            // Essential plan features
            new
            {
                Id = SubscriptionSeedIds.FeatureEssentialMaxUsers,
                PlanId = SubscriptionSeedIds.PlanEssential,
                FeatureKey = "max_users",
                DisplayName = "Maximum users",
                FeatureType = FeatureType.Integer,
                FeatureScope = FeatureScope.PerTenant,
                LimitNumber = 10L,
                IsUnlimited = false
            },
            new
            {
                Id = SubscriptionSeedIds.FeatureEssentialMaxWorkflows,
                PlanId = SubscriptionSeedIds.PlanEssential,
                FeatureKey = "max_workflows",
                DisplayName = "Maximum workflows",
                FeatureType = FeatureType.Integer,
                FeatureScope = FeatureScope.PerTenant,
                LimitNumber = 100L,
                IsUnlimited = false
            },
            new
            {
                Id = SubscriptionSeedIds.FeatureEssentialMaxStorage,
                PlanId = SubscriptionSeedIds.PlanEssential,
                FeatureKey = "max_storage",
                DisplayName = "Maximum storage (bytes)",
                FeatureType = FeatureType.Bytes,
                FeatureScope = FeatureScope.PerTenant,
                LimitNumber = 21_474_836_480L,
                IsUnlimited = false
            },
            new
            {
                Id = SubscriptionSeedIds.FeatureEssentialWorkflowEnabled,
                PlanId = SubscriptionSeedIds.PlanEssential,
                FeatureKey = "workflow_enabled",
                DisplayName = "Workflow module enabled",
                FeatureType = FeatureType.Boolean,
                FeatureScope = FeatureScope.PerTenant,
                LimitBoolean = true,
                IsUnlimited = false
            },
            // Professional plan features
            new
            {
                Id = SubscriptionSeedIds.FeatureProfessionalMaxUsers,
                PlanId = SubscriptionSeedIds.PlanProfessional,
                FeatureKey = "max_users",
                DisplayName = "Maximum users",
                FeatureType = FeatureType.Integer,
                FeatureScope = FeatureScope.PerTenant,
                LimitNumber = 30L,
                IsUnlimited = false
            },
            new
            {
                Id = SubscriptionSeedIds.FeatureProfessionalMaxWorkflows,
                PlanId = SubscriptionSeedIds.PlanProfessional,
                FeatureKey = "max_workflows",
                DisplayName = "Maximum workflows",
                FeatureType = FeatureType.Integer,
                FeatureScope = FeatureScope.PerTenant,
                LimitNumber = 300L,
                IsUnlimited = false
            },
            new
            {
                Id = SubscriptionSeedIds.FeatureProfessionalMaxStorage,
                PlanId = SubscriptionSeedIds.PlanProfessional,
                FeatureKey = "max_storage",
                DisplayName = "Maximum storage (bytes)",
                FeatureType = FeatureType.Bytes,
                FeatureScope = FeatureScope.PerTenant,
                LimitNumber = 85_899_345_920L,
                IsUnlimited = false
            },
            new
            {
                Id = SubscriptionSeedIds.FeatureProfessionalWorkflowEnabled,
                PlanId = SubscriptionSeedIds.PlanProfessional,
                FeatureKey = "workflow_enabled",
                DisplayName = "Workflow module enabled",
                FeatureType = FeatureType.Boolean,
                FeatureScope = FeatureScope.PerTenant,
                LimitBoolean = true,
                IsUnlimited = false
            },
            // Enterprise plan features
            new
            {
                Id = SubscriptionSeedIds.FeatureEnterpriseMaxUsers,
                PlanId = SubscriptionSeedIds.PlanEnterprise,
                FeatureKey = "max_users",
                DisplayName = "Maximum users",
                FeatureType = FeatureType.Integer,
                FeatureScope = FeatureScope.PerTenant,
                IsUnlimited = true
            },
            new
            {
                Id = SubscriptionSeedIds.FeatureEnterpriseMaxWorkflows,
                PlanId = SubscriptionSeedIds.PlanEnterprise,
                FeatureKey = "max_workflows",
                DisplayName = "Maximum workflows",
                FeatureType = FeatureType.Integer,
                FeatureScope = FeatureScope.PerTenant,
                IsUnlimited = true
            },
            new
            {
                Id = SubscriptionSeedIds.FeatureEnterpriseMaxStorage,
                PlanId = SubscriptionSeedIds.PlanEnterprise,
                FeatureKey = "max_storage",
                DisplayName = "Maximum storage (bytes)",
                FeatureType = FeatureType.Bytes,
                FeatureScope = FeatureScope.PerTenant,
                IsUnlimited = true
            },
            new
            {
                Id = SubscriptionSeedIds.FeatureEnterpriseWorkflowEnabled,
                PlanId = SubscriptionSeedIds.PlanEnterprise,
                FeatureKey = "workflow_enabled",
                DisplayName = "Workflow module enabled",
                FeatureType = FeatureType.Boolean,
                FeatureScope = FeatureScope.PerTenant,
                LimitBoolean = true,
                IsUnlimited = true
            });
    }
}