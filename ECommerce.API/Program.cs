using ECommerce.API.Extensions;
using ECommerce.API.Middlewares;
using ECommerce.Infrastructure.Mongo;
using ECommerce.Modules.Products.Infrastructure;

namespace ECommerce.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        AppContext.SetSwitch("System.Net.Security.NoRevocationCheckByDefault", true);

        var builder = WebApplication.CreateBuilder(args);

        var embeddedMongo = await LocalMongo.StartEmbeddedAsync(builder.Configuration, builder.Environment);
        builder.Services.AddSingleton(embeddedMongo);
        Console.WriteLine($"MongoDB: local {builder.Configuration["MongoDb:ConnectionString"]} / {builder.Configuration["MongoDb:DatabaseName"]}");

        Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads"));

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize = 2 * 1024 * 1024 + 64 * 1024;
        });

        builder.Services.AddControllers();
        builder.Services.AddECommerceApi(builder.Configuration);

        var app = builder.Build();

        app.UseMiddleware<ExceptionMiddleware>();
        app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";
            await next();
        });

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseHttpsRedirection();
        }

        app.UseCors("Frontend");
        app.UseRateLimiter();
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = context =>
            {
                context.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                if (context.Context.Request.Path.StartsWithSegments("/uploads"))
                {
                    context.Context.Response.Headers.CacheControl = "public,max-age=86400";
                    context.Context.Response.Headers["Content-Disposition"] = "inline";
                }
            }
        });
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
        app.MapControllers();

        using (var scope = app.Services.CreateScope())
        {
            try
            {
                var mongo = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
                await mongo.EnsureIndexesAsync();
                await CatalogSeed.EnsureAsync(mongo.Database);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Não foi possível conectar no MongoDB local (.mongo-data).",
                    ex);
            }
        }

        await app.RunAsync();
    }
}
