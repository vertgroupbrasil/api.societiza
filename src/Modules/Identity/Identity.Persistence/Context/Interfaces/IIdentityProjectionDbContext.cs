using Core.Persistence.Projection.Abstractions;

namespace Identity.Persistence.Context.Interfaces;

public interface IIdentityProjectionDbContext : IMongoDbContext
{
    Task EnsureIndexesAsync(CancellationToken ct);
}