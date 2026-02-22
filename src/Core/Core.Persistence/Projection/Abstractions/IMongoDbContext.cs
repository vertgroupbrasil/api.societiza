using MongoDB.Driver;

namespace Core.Persistence.Projection.Abstractions;

public interface IMongoDbContext
{
    IMongoClient MongoClient { get; }
    string DatabaseName { get; }

    IMongoCollection<T> GetCollection<T>();
}