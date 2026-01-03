using Core.Application.ServiceLifetimes;
using Core.Persistence.Context;
using Core.Persistence.Repositories;
using Corporate.Domain.Entities;
using Corporate.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Corporate.Persistence.Repositories;

public class AccountancyRepository : Repository<Accountancy>, IAccountancyRepository, IScoped
{
    public AccountancyRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Accountancy?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbSet
            .Include(a => a.Tenant)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }
}