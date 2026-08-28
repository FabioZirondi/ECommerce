using EphemeralMongo;

namespace ECommerce.API;

public static class LocalMongo
{
    public static async Task<IMongoRunner> StartEmbeddedAsync(
        ConfigurationManager configuration,
        IHostEnvironment environment)
    {
        var dataDirectory = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", ".mongo-data"));
        Directory.CreateDirectory(dataDirectory);

        var runner = await MongoRunner.RunAsync(new MongoRunnerOptions
        {
            Version = MongoVersion.V7,
            MongoPort = 27017,
            DataDirectory = dataDirectory
        });

        configuration["MongoDb:ConnectionString"] = runner.ConnectionString;
        configuration["MongoDb:DatabaseName"] = configuration["MongoDb:DatabaseName"] ?? "ecommerce";
        return runner;
    }
}
