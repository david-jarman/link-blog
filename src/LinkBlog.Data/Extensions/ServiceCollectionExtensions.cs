using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LinkBlog.Data.Extensions;

public static class ServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddPostStore(this IHostApplicationBuilder app)
    {
        // REVIEW: Do we need a memory cache for storing posts in memory?
        // How much memory does a post take up?
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