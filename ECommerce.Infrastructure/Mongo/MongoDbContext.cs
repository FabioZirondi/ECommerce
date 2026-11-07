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

        var products = Database.GetCollection<BsonDocument>("products");
        await products.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("sellerId"),
                    new CreateIndexOptions { Name = "ix_products_sellerId" }),
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys
                        .Ascending("status")
                        .Ascending("categoryId")
                        .Descending("createdAt"),
                    new CreateIndexOptions { Name = "ix_products_catalog" })
            ],
            cancellationToken: cancellationToken);

        var orders = Database.GetCollection<BsonDocument>("orders");
        await orders.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("buyerId").Descending("createdAt"),
                    new CreateIndexOptions { Name = "ix_orders_buyerId" }),
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("sellerId").Descending("createdAt"),
                    new CreateIndexOptions { Name = "ix_orders_sellerId" })
            ],
            cancellationToken: cancellationToken);

        var carts = Database.GetCollection<BsonDocument>("carts");
        await carts.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("userId"),
                new CreateIndexOptions { Unique = true, Name = "ux_carts_userId" }),
            cancellationToken: cancellationToken);
    }
}
