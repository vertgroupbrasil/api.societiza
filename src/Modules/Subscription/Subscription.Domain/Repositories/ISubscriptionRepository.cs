using Core.Persistence.Repositories;

namespace Subscription.Domain.Repositories;

public interface ISubscriptionRepository : IRepository<Entities.Subscription>
{
    Task<bool> ExistsActiveOrTrialAsync(string tenantId, CancellationToken cancellationToken);
}