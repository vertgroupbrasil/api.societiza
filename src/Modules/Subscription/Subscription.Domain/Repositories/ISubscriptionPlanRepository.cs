using Subscription.Domain.Entities;

namespace Subscription.Domain.Repositories;

public interface ISubscriptionPlanRepository
{
    Task<SubscriptionPlan?> GetByCodeAsync(string planCode, CancellationToken cancellationToken);
}