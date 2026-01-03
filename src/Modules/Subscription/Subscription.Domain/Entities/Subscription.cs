using Core.Domain.Entities;
using Subscription.Domain.Enumerations;

namespace Subscription.Domain.Entities;

public class Subscription : Entity
{
    public Guid PlanId { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public BillingCycle BillingCycle { get; private set; }

    public DateTimeOffset StartDate { get; private set; }
    public DateTimeOffset? TrialEndDate { get; private set; }
    public DateTimeOffset NextBillingDate { get; private set; }
    public DateTimeOffset? CanceledAt { get; private set; }

    public decimal MonthlyAmount { get; private set; }
    public string Currency { get; private set; } = "BRL";

    public string? AbacatePaySubscriptionId { get; private set; }

    public virtual SubscriptionPlan Plan { get; private set; } = null!;
    public virtual Tenant Tenant { get; private set; } = null!;

    private Subscription() { }

    public static Subscription StartTrial(string tenantId, Guid trialPlanId)
    {
        return new Subscription
        {
            TenantId = tenantId,
            PlanId = trialPlanId,
            Status = SubscriptionStatus.Trial,
            BillingCycle = BillingCycle.Monthly,
            StartDate = DateTimeOffset.UtcNow,
            TrialEndDate = DateTimeOffset.UtcNow.AddDays(14),
            NextBillingDate = DateTimeOffset.UtcNow.AddDays(14),
            MonthlyAmount = 0m
        };
    }

    public void Activate(string abacatePaySubscriptionId, decimal monthlyAmount)
    {
        Status = SubscriptionStatus.Active;
        AbacatePaySubscriptionId = abacatePaySubscriptionId;
        MonthlyAmount = monthlyAmount;
        NextBillingDate = DateTimeOffset.UtcNow.AddMonths(1);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel(string reason)
    {
        Status = SubscriptionStatus.Canceled;
        CanceledAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsActive() => Status is SubscriptionStatus.Active or SubscriptionStatus.Trial;
    public bool IsInTrial() => Status == SubscriptionStatus.Trial && DateTimeOffset.UtcNow < TrialEndDate;
}