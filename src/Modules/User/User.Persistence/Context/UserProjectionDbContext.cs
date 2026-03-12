using Core.Persistence.Projection.Abstractions;
using User.Persistence.Context.Interfaces;

namespace User.Persistence.Context;

public class UserProjectionDbContext(string connectionString) : MongoDbContext(connectionString), IUserProjectionDbContext
{
}