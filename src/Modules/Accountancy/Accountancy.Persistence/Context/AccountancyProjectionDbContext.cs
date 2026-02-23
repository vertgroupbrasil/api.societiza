using Core.Persistence.Projection.Abstractions;
using Accountancy.Persistence.Context.Interfaces;

namespace Accountancy.Persistence.Context;

public class AccountancyProjectionDbContext(string connectionString) : MongoDbContext(connectionString), IAccountancyProjectionDbContext
{
}