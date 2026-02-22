using Core.Persistence.Projection.Abstractions;
using Corporate.Persistence.Context.Interfaces;

namespace Corporate.Persistence.Context;

public class CorporateProjectionDbContext(string connectionString) : MongoDbContext(connectionString), ICorporateProjectionDbContext
{
}