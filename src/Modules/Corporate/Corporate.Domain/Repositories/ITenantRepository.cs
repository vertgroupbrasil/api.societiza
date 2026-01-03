using Core.Domain.Entities;
using System.Linq.Expressions;

namespace Corporate.Domain.Repositories;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Tenant?> GetByIdAsync(string id, params Expression<Func<Tenant, object>>[] includes);
    Task<IEnumerable<Tenant>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Tenant>> FindAsync(Expression<Func<Tenant, bool>> predicate, CancellationToken cancellationToken = default);
    Task<Tenant?> FirstOrDefaultAsync(Expression<Func<Tenant, bool>> predicate, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Expression<Func<Tenant, bool>> predicate, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<Tenant, bool>>? predicate = null, CancellationToken cancellationToken = default);

    void Add(Tenant entity);
    void Update(Tenant entity);
    void Remove(Tenant entity);
}