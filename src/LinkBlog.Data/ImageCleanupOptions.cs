namespace LinkBlog.Data;

/// <summary>
/// Configuration options for the orphaned image cleanup service.
/// </summary>
public sealed class ImageCleanupOptions
{
    /// <summary>
    /// Gets or sets the interval at which orphaned images are cleaned up.
    /// Default is 1 hour.
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets whether to enable the orphaned image cleanup service.
    /// </summary>
    public bool EnableCleanup { get; set; } = true;
}