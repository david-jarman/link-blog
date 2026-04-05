using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LinkBlog.Data.Extensions;

public static class ServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddPostStore(this IHostApplicationBuilder app)
    {
        app.Services.AddMemoryCache(options =>
        {
            options.SizeLimit = 100;
        });

        app.Services.AddSingleton<PostMarkdownSerializer>();
        app.Services.AddScoped<IPostDataAccess, MarkdownPostDataAccess>();
        app.Services.AddScoped<IPostStore, CachedPostStore>();
        app.Services.AddHostedService<PostCacheRefreshService>();

        return app;
    }
}