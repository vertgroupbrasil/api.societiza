using Core.Application.ServiceLifetimes;
using Core.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Subscription.Domain.Entities;
using Subscription.Domain.Repositories;

namespace Subscription.Persistence.Repositories;

public class SubscriptionPlanRepository : ISubscriptionPlanRepository, IScoped
{
    private readonly DbSet<SubscriptionPlan> _dbSet;

    public SubscriptionPlanRepository(ApplicationDbContext context)
    {
        _dbSet = context.Set<SubscriptionPlan>();
    }

    public async Task<SubscriptionPlan?> GetByCodeAsync(string planCode, CancellationToken cancellationToken)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(sp => sp.Code == planCode, cancellationToken);
    }
}