using Aspire.Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LinkBlog.Data.Extensions;

public static class ServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddPostStore(this IHostApplicationBuilder app, string connectionName, Action<NpgsqlEntityFrameworkCorePostgreSQLSettings>? configureSettings)
    {
        app.AddNpgsqlDbContext<PostDbContext>(connectionName, configureSettings, dbContextBuilder =>
        {
            if (app.Environment.IsDevelopment())
            {
                dbContextBuilder.EnableSensitiveDataLogging();
            }
        });

        // Register memory cache
        app.Services.AddMemoryCache(options =>
        {
            options.SizeLimit = 100; // Limit cache to 100 items (posts cache counts as 1)
        });

        // Register PostDataAccess for database operations
        app.Services.AddScoped<IPostDataAccess, PostDataAccess>();

        // Register CachedPostStore as the IPostStore implementation
        app.Services.AddScoped<IPostStore, CachedPostStore>();

        // Register background service for periodic cache refresh
        app.Services.AddHostedService<PostCacheRefreshService>();

        return app;
    }
}
