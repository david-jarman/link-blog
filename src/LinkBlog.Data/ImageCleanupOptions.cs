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

    /// <summary>
    /// Gets or sets the minimum age an image must be before it can be deleted.
    /// This prevents race conditions where images are uploaded but not yet referenced.
    /// Default is 8 hours.
    /// </summary>
    public TimeSpan MinimumImageAge { get; set; } = TimeSpan.FromHours(8);
}