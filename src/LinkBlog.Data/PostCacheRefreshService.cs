using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LinkBlog.Data;

/// <summary>
/// Background service that periodically refreshes the post cache.
/// Runs every 5 minutes by default to keep the cache fresh.
/// </summary>
public sealed class PostCacheRefreshService : BackgroundService
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<PostCacheRefreshService> logger;
    private readonly TimeSpan refreshInterval;

    public PostCacheRefreshService(
        IServiceProvider serviceProvider,
        ILogger<PostCacheRefreshService> logger)
    {
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.refreshInterval = TimeSpan.FromMinutes(5); // Configurable refresh interval
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.logger.LogInformation("Post cache refresh service starting. Refresh interval: {RefreshInterval}", this.refreshInterval);

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
            // Create a scope to resolve scoped services (PostStoreDb is scoped)
            using var scope = this.serviceProvider.CreateScope();
            var postStore = scope.ServiceProvider.GetRequiredService<IPostStore>();

            // Only refresh if the store is CachedPostStore
            if (postStore is CachedPostStore cachedStore)
            {
                await cachedStore.RefreshCacheAsync(cancellationToken);
            }
            else
            {
                this.logger.LogWarning("IPostStore is not CachedPostStore, skipping cache refresh");
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to refresh post cache");
            throw;
        }
    }
}
