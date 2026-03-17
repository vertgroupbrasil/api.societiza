using Core.Persistence.Projection.Abstractions;
using Identity.Persistence.Context.Interfaces;

namespace Identity.Persistence.Context;

public class IdentityProjectionDbContext(string connectionString) : MongoDbContext(connectionString), IIdentityProjectionDbContext
{
}