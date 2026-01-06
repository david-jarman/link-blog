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
    private readonly TimeSpan cleanupInterval;
    private readonly bool enableCleanup;

    [GeneratedRegex(@"https?://[^/]+/images/[^\s""'<>]+", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ImageUrlRegex();

    public OrphanedImageCleanupService(
        IServiceProvider serviceProvider,
        BlobServiceClient blobServiceClient,
        ILogger<OrphanedImageCleanupService> logger,
        IOptions<ImageCleanupOptions> options)
    {
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        this.blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(options);
        this.cleanupInterval = options.Value.CleanupInterval;
        this.enableCleanup = options.Value.EnableCleanup;
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
        }

        // Wait before first cleanup to allow the application to fully start
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        // Periodic cleanup loop
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await this.CleanupOrphanedImagesAsync(stoppingToken);
                await Task.Delay(this.cleanupInterval, stoppingToken);
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

    private async Task CleanupOrphanedImagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            this.logger.LogInformation("Starting orphaned image cleanup");

            // Get all blob URLs from Azure Storage
            var blobUrls = await this.GetAllBlobUrlsAsync(cancellationToken);
            this.logger.LogInformation("Found {BlobCount} images in blob storage", blobUrls.Count);

            // Get all image URLs referenced in posts
            var referencedUrls = await this.GetReferencedImageUrlsAsync(cancellationToken);
            this.logger.LogInformation("Found {ReferencedCount} images referenced in posts", referencedUrls.Count);

            // Find orphaned images (blobs not referenced by any post)
            var orphanedUrls = blobUrls.Except(referencedUrls, StringComparer.OrdinalIgnoreCase).ToList();

            if (orphanedUrls.Count == 0)
            {
                this.logger.LogInformation("No orphaned images found");
                return;
            }

            this.logger.LogInformation("Found {OrphanedCount} orphaned images to delete", orphanedUrls.Count);

            // Delete orphaned blobs
            var containerClient = this.blobServiceClient.GetBlobContainerClient("images");
            int deletedCount = 0;

            foreach (var url in orphanedUrls)
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
                        this.logger.LogWarning("Could not extract blob name from URL: {Url}", url);
                        continue;
                    }

                    var blobClient = containerClient.GetBlobClient(blobName);
                    var deleted = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

                    if (deleted.Value)
                    {
                        deletedCount++;
                        this.logger.LogInformation("Deleted orphaned image: {BlobName}", blobName);
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Failed to delete orphaned image: {Url}", url);
                }
            }

            this.logger.LogInformation("Orphaned image cleanup completed. Deleted {DeletedCount} of {OrphanedCount} orphaned images", deletedCount, orphanedUrls.Count);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to cleanup orphaned images");
            throw;
        }
    }

    private async Task<HashSet<string>> GetAllBlobUrlsAsync(CancellationToken cancellationToken)
    {
        var blobUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var containerClient = this.blobServiceClient.GetBlobContainerClient("images");

        // Check if container exists
        if (!await containerClient.ExistsAsync(cancellationToken))
        {
            this.logger.LogWarning("Images container does not exist");
            return blobUrls;
        }

        await foreach (var blobItem in containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            var blobClient = containerClient.GetBlobClient(blobItem.Name);
            blobUrls.Add(blobClient.Uri.AbsoluteUri);
        }

        return blobUrls;
    }

    private async Task<HashSet<string>> GetReferencedImageUrlsAsync(CancellationToken cancellationToken)
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
