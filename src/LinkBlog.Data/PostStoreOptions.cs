namespace LinkBlog.Data;

/// <summary>
/// Configuration options for the post store.
/// </summary>
public sealed class PostStoreOptions
{
    /// <summary>
    /// Gets or sets the interval at which the post cache is refreshed.
    /// Default is 5 minutes.
    /// </summary>
    public TimeSpan CacheRefreshInterval { get; set; } = TimeSpan.FromMinutes(5);
}