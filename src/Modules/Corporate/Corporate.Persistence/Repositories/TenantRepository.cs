using Core.Application.ServiceLifetimes;
using Core.Domain.Entities;
using Core.Persistence.Context;
using Corporate.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Corporate.Persistence.Repositories;

public class TenantRepository : ITenantRepository, IScoped
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<Tenant> _dbSet;

    public TenantRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<Tenant>();
    }

    public async Task<Tenant?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<Tenant?> GetByIdAsync(string id, params Expression<Func<Tenant, object>>[] includes)
    {
        IQueryable<Tenant> query = _dbSet;

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Tenant>> FindAsync(Expression<Func<Tenant, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    public async Task<Tenant?> FirstOrDefaultAsync(Expression<Func<Tenant, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Expression<Func<Tenant, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }

    public async Task<int> CountAsync(Expression<Func<Tenant, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        if (predicate == null)
            return await _dbSet.CountAsync(cancellationToken);

        return await _dbSet.CountAsync(predicate, cancellationToken);
    }

    public void Add(Tenant entity)
    {
        _dbSet.Add(entity);
    }

    public void Update(Tenant entity)
    {
        _dbSet.Attach(entity);
        _context.Entry(entity).State = EntityState.Modified;
    }

    public void Remove(Tenant entity)
    {
        if (_context.Entry(entity).State == EntityState.Detached)
        {
            _dbSet.Attach(entity);
        }

        _dbSet.Remove(entity);
    }
}