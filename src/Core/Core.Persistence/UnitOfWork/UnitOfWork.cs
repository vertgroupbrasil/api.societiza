using Core.Application.ServiceLifetimes;
using Core.Application.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Core.Persistence.UnitOfWork;

public class UnitOfWork<TContext>(TContext dbContext) : IUnitOfWork<TContext>, IScoped
        where TContext : DbContext
{
    private readonly DatabaseFacade _database = dbContext.Database;

    public Task ExecuteAsync(Func<CancellationToken, Task> operationAsync, CancellationToken cancellationToken)
        => _database.CreateExecutionStrategy().ExecuteAsync(ct => ExecuteTransactionAsync(operationAsync, ct), cancellationToken);

    private async Task ExecuteTransactionAsync(Func<CancellationToken, Task> operationAsync, CancellationToken cancellationToken)
    {
        await using var transaction = await _database.BeginTransactionAsync(cancellationToken);

        try
        {
            await operationAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}