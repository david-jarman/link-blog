using System.Text.RegularExpressions;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkBlog.Data;

/// <summary>
/// Background service that periodically cleans up orphaned images from Azure Blob Storage.
/// An orphaned image is one that is not referenced by any post (archived or live).
/// </summary>
public sealed partial class OrphanedImageCleanupService : BackgroundService
{
    private readonly IServiceProvider serviceProvider;
    private readonly BlobServiceClient blobServiceClient;
    private readonly ILogger<OrphanedImageCleanupService> logger;
    private readonly IDelayService delayService;
    private readonly TimeSpan cleanupInterval;
    private readonly TimeSpan minimumImageAge;
    private readonly bool enableCleanup;
    private readonly bool dryRun;

    [GeneratedRegex(@"https?://[^/]+/images/[^\s""'<>]+", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ImageUrlRegex();

    [LoggerMessage(Level = LogLevel.Information, Message = "Found {blobCount} images in blob storage")]
    private partial void LogBlobCount(int blobCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Found {referencedCount} images referenced in posts")]
    private partial void LogReferencedCount(int referencedCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Found {orphanedCount} orphaned images to delete")]
    private partial void LogOrphanedCount(int orphanedCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not extract blob name from URL: {url}")]
    private partial void LogCouldNotExtractBlobName(string url);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted orphaned image: {blobName}")]
    private partial void LogDeletedImage(string blobName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to delete orphaned image: {url}")]
    private partial void LogFailedToDeleteImage(Exception ex, string url);

    [LoggerMessage(Level = LogLevel.Information, Message = "Filtered out {filteredCount} orphaned images that are less than {minimumAge} old")]
    private partial void LogFilteredRecentImages(int filteredCount, TimeSpan minimumAge);

    [LoggerMessage(Level = LogLevel.Information, Message = "Orphaned image cleanup completed. Deleted {deletedCount} of {orphanedCount} orphaned images")]
    private partial void LogCleanupCompleted(int deletedCount, int orphanedCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "[DRY RUN] Would delete orphaned image: {blobName}")]
    private partial void LogDryRunWouldDeleteImage(string blobName);

    [LoggerMessage(Level = LogLevel.Information, Message = "[DRY RUN] Orphaned image cleanup completed. Would have deleted {deletedCount} of {orphanedCount} orphaned images")]
    private partial void LogDryRunCleanupCompleted(int deletedCount, int orphanedCount);

    public OrphanedImageCleanupService(
        IServiceProvider serviceProvider,
        BlobServiceClient blobServiceClient,
        ILogger<OrphanedImageCleanupService> logger,
        IDelayService delayService,
        IOptions<ImageCleanupOptions> options)
    {
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        this.blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.delayService = delayService ?? throw new ArgumentNullException(nameof(delayService));
        ArgumentNullException.ThrowIfNull(options);
        this.cleanupInterval = options.Value.CleanupInterval;
        this.minimumImageAge = options.Value.MinimumImageAge;
        this.enableCleanup = options.Value.EnableCleanup;
        this.dryRun = options.Value.DryRun;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!this.enableCleanup)
        {
            this.logger.LogInformation("Orphaned image cleanup service is disabled");
            return;
        }

        if (this.logger.IsEnabled(LogLevel.Information))
        {
            this.logger.LogInformation("Orphaned image cleanup service starting. Cleanup interval: {CleanupInterval}", this.cleanupInterval);
            if (this.dryRun)
            {
                this.logger.LogInformation("DRY RUN MODE ENABLED - No images will actually be deleted");
            }
        }

        // Wait before first cleanup to allow the application to fully start
        await this.delayService.DelayAsync(TimeSpan.FromMinutes(1), stoppingToken);

        // Periodic cleanup loop
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await this.CleanupOrphanedImagesAsync(stoppingToken);
                await this.delayService.DelayAsync(this.cleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when the service is stopping
                this.logger.LogInformation("Orphaned image cleanup service is stopping");
                break;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error in orphaned image cleanup service");
                // Continue running even if one cleanup fails
            }
        }

        this.logger.LogInformation("Orphaned image cleanup service stopped");
    }

    internal async Task CleanupOrphanedImagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            this.logger.LogInformation("Starting orphaned image cleanup");

            // Get all blob URLs from Azure Storage with their creation times
            var blobInfo = await this.GetAllBlobUrlsAsync(cancellationToken);
            this.LogBlobCount(blobInfo.Count);

            // Get all image URLs referenced in posts
            var referencedUrls = await this.GetReferencedImageUrlsAsync(cancellationToken);
            this.LogReferencedCount(referencedUrls.Count);

            // Find orphaned images (blobs not referenced by any post)
            var orphanedUrls = blobInfo.Keys.Except(referencedUrls, StringComparer.OrdinalIgnoreCase).ToList();

            if (orphanedUrls.Count == 0)
            {
                this.logger.LogInformation("No orphaned images found");
                return;
            }

            this.LogOrphanedCount(orphanedUrls.Count);

            // Filter out blobs that are too recent to prevent race conditions
            var now = DateTimeOffset.UtcNow;
            var oldOrphanedUrls = orphanedUrls
                .Where(url => blobInfo.TryGetValue(url, out var createdOn) && (now - createdOn) >= this.minimumImageAge)
                .ToList();

            var filteredCount = orphanedUrls.Count - oldOrphanedUrls.Count;
            if (filteredCount > 0)
            {
                this.LogFilteredRecentImages(filteredCount, this.minimumImageAge);
            }

            if (oldOrphanedUrls.Count == 0)
            {
                this.logger.LogInformation("No orphaned images old enough to delete");
                return;
            }

            // Delete orphaned blobs (or log what would be deleted in dry run mode)
            var containerClient = this.blobServiceClient.GetBlobContainerClient("images");
            int deletedCount = 0;

            foreach (var url in oldOrphanedUrls)
            {
                try
                {
                    // Extract blob name from URL
                    // URL format: https://{account}.blob.core.windows.net/images/{blobName}
                    var uri = new Uri(url);
                    var pathParts = uri.AbsolutePath.Split("/images/", 2, StringSplitOptions.RemoveEmptyEntries);
                    var blobName = pathParts.Length > 0 ? pathParts[^1] : null;

                    if (string.IsNullOrEmpty(blobName))
                    {
                        this.LogCouldNotExtractBlobName(url);
                        continue;
                    }

                    if (this.dryRun)
                    {
                        // Dry run mode - just log what would be deleted
                        deletedCount++;
                        this.LogDryRunWouldDeleteImage(blobName);
                    }
                    else
                    {
                        // Actually delete the blob
                        var blobClient = containerClient.GetBlobClient(blobName);
                        var deleted = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

                        if (deleted.Value)
                        {
                            deletedCount++;
                            this.LogDeletedImage(blobName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.LogFailedToDeleteImage(ex, url);
                }
            }

            if (this.dryRun)
            {
                this.LogDryRunCleanupCompleted(deletedCount, oldOrphanedUrls.Count);
            }
            else
            {
                this.LogCleanupCompleted(deletedCount, oldOrphanedUrls.Count);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to cleanup orphaned images");
            throw;
        }
    }

    internal async Task<Dictionary<string, DateTimeOffset>> GetAllBlobUrlsAsync(CancellationToken cancellationToken)
    {
        var blobInfo = new Dictionary<string, DateTimeOffset>(StringComparer.OrdinalIgnoreCase);
        var containerClient = this.blobServiceClient.GetBlobContainerClient("images");

        // Check if container exists
        if (!await containerClient.ExistsAsync(cancellationToken))
        {
            this.logger.LogWarning("Images container does not exist");
            return blobInfo;
        }

        await foreach (var blobItem in containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            var blobClient = containerClient.GetBlobClient(blobItem.Name);
            var createdOn = blobItem.Properties?.CreatedOn ?? DateTimeOffset.UtcNow;
            blobInfo[blobClient.Uri.AbsoluteUri] = createdOn;
        }

        return blobInfo;
    }

    internal async Task<HashSet<string>> GetReferencedImageUrlsAsync(CancellationToken cancellationToken)
    {
        var referencedUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Create a scope to resolve scoped services (PostDbContext is scoped)
        using var scope = this.serviceProvider.CreateScope();
        var postDbContext = scope.ServiceProvider.GetRequiredService<PostDbContext>();

        // Get all posts (both archived and non-archived)
        var posts = await postDbContext.Posts
            .Select(p => p.Contents)
            .ToListAsync(cancellationToken);

        // Extract image URLs from post contents using regex
        foreach (var contents in posts)
        {
            if (string.IsNullOrEmpty(contents))
            {
                continue;
            }

            var matches = ImageUrlRegex().Matches(contents);
            foreach (Match match in matches)
            {
                referencedUrls.Add(match.Value);
            }
        }

        return referencedUrls;
    }
}