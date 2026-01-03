using Microsoft.EntityFrameworkCore;

namespace Core.Persistence.UnitOfWork;

public interface IUnitOfWork<TContext> where TContext : DbContext
{
    Task ExecuteAsync(Func<CancellationToken, Task> operationAsync, CancellationToken cancellationToken);
}