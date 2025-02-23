using Aspire.Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LinkBlog.Data.Extensions;

public static class ServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddPostStore(this IHostApplicationBuilder app, string connectionName, Action<NpgsqlEntityFrameworkCorePostgreSQLSettings>? configureSettings)
    {
        app.AddNpgsqlDbContext<PostDbContext>(connectionName, configureSettings);
        app.Services.AddScoped<IPostStore, PostStoreDb>();

        return app;
    }
}