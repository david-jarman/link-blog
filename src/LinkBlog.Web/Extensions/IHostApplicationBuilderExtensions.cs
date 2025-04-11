using Aspire.Npgsql.EntityFrameworkCore.PostgreSQL;

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
        app.Services.AddScoped<IPostStore, PostStoreDb>();

        return app;
    }
}