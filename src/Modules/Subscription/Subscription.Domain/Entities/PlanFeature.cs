using Core.Domain.Entities;
using Subscription.Domain.Enumerations;

namespace Subscription.Domain.Entities;

public class PlanFeature : Global
{
    public Guid PlanId { get; private set; }
    public string FeatureKey { get; private set; } = string.Empty; // "max_users", "max_workflows"
    public string DisplayName { get; private set; } = string.Empty; // "Maximum Users", "Maximum Workflows"

    public FeatureType FeatureType { get; private set; }
    public FeatureScope FeatureScope { get; private set; }

    public bool? LimitBoolean { get; private set; }
    public long? LimitNumber { get; private set; }
    public string? LimitString { get; private set; }
    public string? LimitJson { get; private set; }

    public bool IsUnlimited { get; private set; }

    public virtual SubscriptionPlan Plan { get; private set; } = null!;

    private PlanFeature() { }
}