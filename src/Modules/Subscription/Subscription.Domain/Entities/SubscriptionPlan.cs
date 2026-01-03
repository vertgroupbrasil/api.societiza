using Core.Domain.Entities;

namespace Subscription.Domain.Entities;

public class SubscriptionPlan : Global
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    public decimal MonthlyPrice { get; private set; }
    public decimal YearlyPrice { get; private set; }

    public bool IsActive { get; private set; } = true;
    public bool IsPublic { get; private set; } = true;

    private readonly List<PlanFeature> _features = new();
    public IReadOnlyCollection<PlanFeature> Features => _features.AsReadOnly();

    public static SubscriptionPlan Create(string code, string name, decimal monthlyPrice, decimal yearlyPrice)
    {
        return new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            MonthlyPrice = monthlyPrice,
            YearlyPrice = yearlyPrice
        };
    }
}