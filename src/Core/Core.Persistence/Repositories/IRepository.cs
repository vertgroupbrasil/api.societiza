using Core.Domain.Entities;
using System.Linq.Expressions;

namespace Core.Persistence.Repositories;

public interface IRepository<TEntity> where TEntity : Entity
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TEntity?> GetByIdAsync(Guid id, params Expression<Func<TEntity, object>>[] includes);
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);

    void Add(TEntity entity);
    void Update(TEntity entity);
    void Remove(TEntity entity);

    void AddRange(IEnumerable<TEntity> entities);
    void UpdateRange(IEnumerable<TEntity> entities);
    void RemoveRange(IEnumerable<TEntity> entities);

    IQueryable<TEntity> GetQueryable();
}