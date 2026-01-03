using Core.Persistence.Repositories;
using Corporate.Domain.Entities;

namespace Corporate.Domain.Repositories;

public interface IAccountancyRepository : IRepository<Accountancy>
{
    Task<Accountancy?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken); 
}