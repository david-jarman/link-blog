using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkBlog.Data;

/// <summary>
/// Background service that periodically refreshes the post cache.
/// </summary>
public sealed class PostCacheRefreshService : BackgroundService
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<PostCacheRefreshService> logger;
    private readonly TimeSpan refreshInterval;

    public PostCacheRefreshService(
        IServiceProvider serviceProvider,
        ILogger<PostCacheRefreshService> logger,
        IOptions<PostStoreOptions> options)
    {
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(options);
        this.refreshInterval = options.Value.CacheRefreshInterval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (this.logger.IsEnabled(LogLevel.Information))
        {
            this.logger.LogInformation("Post cache refresh service starting. Refresh interval: {RefreshInterval}", refreshInterval);
        }

        // Initial cache warm-up on startup
        await this.RefreshCacheAsync(stoppingToken);

        // Periodic refresh loop
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(this.refreshInterval, stoppingToken);
                await this.RefreshCacheAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when the service is stopping
                this.logger.LogInformation("Post cache refresh service is stopping");
                break;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error in post cache refresh service");
                // Continue running even if one refresh fails
            }
        }

        this.logger.LogInformation("Post cache refresh service stopped");
    }

    private async Task RefreshCacheAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Create a scope to resolve scoped services
            using var scope = this.serviceProvider.CreateScope();
            var cachedStore = (CachedPostStore)scope.ServiceProvider.GetRequiredService<IPostStore>();
            await cachedStore.RefreshCacheAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to refresh post cache");
            throw;
        }
    }
}