using Aspire.Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LinkBlog.Data.Extensions;

public static class ServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddPostStore(this IHostApplicationBuilder app, string connectionName, Action<NpgsqlEntityFrameworkCorePostgreSQLSettings>? configureSettings, bool enableInMemoryCache = false)
    {
        app.AddNpgsqlDbContext<PostDbContext>(connectionName, configureSettings, dbContextBuilder =>
        {
            if (app.Environment.IsDevelopment())
            {
                dbContextBuilder.EnableSensitiveDataLogging();
            }
        });

        if (enableInMemoryCache)
        {
            // Register memory cache if not already registered
            app.Services.AddMemoryCache(options =>
            {
                options.SizeLimit = 100; // Limit cache to 100 items (posts cache counts as 1)
            });

            // Register PostStoreDb as itself so it can be injected into CachedPostStore
            app.Services.AddScoped<PostStoreDb>();

            // Register CachedPostStore as the IPostStore implementation
            app.Services.AddScoped<IPostStore, CachedPostStore>();

            // Register background service for periodic cache refresh
            app.Services.AddHostedService<PostCacheRefreshService>();
        }
        else
        {
            // Register PostStoreDb directly as IPostStore (original behavior)
            app.Services.AddScoped<IPostStore, PostStoreDb>();
        }

        return app;
    }
}