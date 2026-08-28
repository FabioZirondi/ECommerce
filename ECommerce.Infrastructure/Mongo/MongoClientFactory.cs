using MongoDB.Driver;

namespace ECommerce.Infrastructure.Mongo;

public static class MongoClientFactory
{
    public static MongoClient Create(string connectionString)
    {
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);
        settings.ConnectTimeout = TimeSpan.FromSeconds(10);
        return new MongoClient(settings);
    }
}
