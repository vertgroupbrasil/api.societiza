using Core.Persistence.Projection.Abstractions;
using Identity.Domain.Projection;
using Identity.Persistence.Context.Interfaces;
using MongoDB.Driver;

namespace Identity.Persistence.Context;

public class IdentityProjectionDbContext(string connectionString) : MongoDbContext(connectionString), IIdentityProjectionDbContext
{
    public async Task EnsureIndexesAsync(CancellationToken ct)
    {
        var collection = GetCollection<ProjectionModel.InvitationLink>();

        var indexKeys = Builders<ProjectionModel.InvitationLink>.IndexKeys
            .Ascending(x => x.TargetRole)
            .Ascending(x => x.AccountancyId);

        var indexOptions = new CreateIndexOptions<ProjectionModel.InvitationLink>
        {
            Unique = true,
            PartialFilterExpression = Builders<ProjectionModel.InvitationLink>.Filter
                .Eq(x => x.Status, "Active")
        };

        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<ProjectionModel.InvitationLink>(indexKeys, indexOptions),
            cancellationToken: ct);
    }
}