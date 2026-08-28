using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace ECommerce.Infrastructure.Mongo;

public sealed class MongoDbContext
{
    static MongoDbContext()
    {
        var pack = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new IgnoreExtraElementsConvention(true),
            new EnumRepresentationConvention(BsonType.String)
        };
        ConventionRegistry.Register("ecommerce", pack, _ => true);
    }

    public IMongoDatabase Database { get; }

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = MongoClientFactory.Create(settings.Value.ConnectionString);
        Database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        var users = Database.GetCollection<BsonDocument>("users");
        var emailIndex = new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("email"),
            new CreateIndexOptions { Unique = true, Name = "ux_users_email" });

        await users.Indexes.CreateOneAsync(emailIndex, cancellationToken: cancellationToken);
    }
}
