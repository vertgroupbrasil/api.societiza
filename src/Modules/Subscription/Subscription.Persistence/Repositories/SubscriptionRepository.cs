using Core.Application.ServiceLifetimes;
using Core.Persistence.Context;
using Core.Persistence.Repositories;
using Subscription.Domain.Enumerations;
using Subscription.Domain.Repositories;

namespace Subscription.Persistence.Repositories;

public class SubscriptionRepository : Repository<Domain.Entities.Subscription>, ISubscriptionRepository, IScoped
{
    public SubscriptionRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<bool> ExistsActiveOrTrialAsync(string tenantId, CancellationToken cancellationToken)
    {
        return await ExistsAsync(s => s.TenantId == tenantId &&
            (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial), cancellationToken);
    }
}