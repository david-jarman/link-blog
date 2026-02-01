using System.Runtime.CompilerServices;
using LinkBlog.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LinkBlog.Data;

/// <summary>
/// In-memory cached implementation of IPostStore.
/// Caches all posts in memory and provides fast read access with cache invalidation on writes.
/// </summary>
public sealed class CachedPostStore : IPostStore, IDisposable
{
    private const string AllPostsCacheKey = "CachedPostStore_AllPosts";
    private readonly IPostDataAccess dataAccess;
    private readonly IMemoryCache memoryCache;
    private readonly ILogger<CachedPostStore> logger;
    private readonly SemaphoreSlim cacheLock = new SemaphoreSlim(1, 1);

    public CachedPostStore(
        IPostDataAccess dataAccess,
        IMemoryCache memoryCache,
        ILogger<CachedPostStore> logger)
    {
        this.dataAccess = dataAccess ?? throw new ArgumentNullException(nameof(dataAccess));
        this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Refreshes the entire post cache from the database.
    /// Called periodically by the background service and on cache invalidation.
    /// </summary>
    public async Task RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        await this.cacheLock.WaitAsync(cancellationToken);
        try
        {
            this.logger.LogInformation("Refreshing post cache from database...");

            var posts = new List<Post>();
            await foreach (var post in this.dataAccess.GetAllPostsAsync(cancellationToken))
            {
                posts.Add(post);
            }

            var cacheOptions = new MemoryCacheEntryOptions
            {
                Priority = CacheItemPriority.NeverRemove,
                Size = 1
            };

            this.memoryCache.Set(AllPostsCacheKey, posts, cacheOptions);

            if (this.logger.IsEnabled(LogLevel.Information))
            {
                var postCount = posts.Count;
                this.logger.LogInformation("Post cache refreshed with {PostCount} posts", postCount);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error refreshing post cache");
            throw;
        }
        finally
        {
            this.cacheLock.Release();
        }
    }

    /// <summary>
    /// Invalidates the cache, forcing a refresh on next access.
    /// </summary>
    private void InvalidateCache()
    {
        this.memoryCache.Remove(AllPostsCacheKey);
        this.logger.LogInformation("Post cache invalidated");
    }

    /// <summary>
    /// Gets all cached posts or loads them from database if not cached.
    /// </summary>
    private async Task<List<Post>> GetCachedPostsAsync(CancellationToken cancellationToken = default)
    {
        if (this.memoryCache.TryGetValue<List<Post>>(AllPostsCacheKey, out var cachedPosts) && cachedPosts != null)
        {
            return cachedPosts;
        }

        // Cache miss - load from database
        await this.RefreshCacheAsync(cancellationToken);

        return this.memoryCache.Get<List<Post>>(AllPostsCacheKey) ?? new List<Post>();
    }

    public async IAsyncEnumerable<Post> GetPosts(int topN, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var posts = await this.GetCachedPostsAsync(cancellationToken);

        var results = posts
            .OrderByDescending(p => p.CreatedDate)
            .Take(topN);

        foreach (var post in results)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return post;
        }
    }

    public async Task<PagedPostsResult> GetPostsPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            page = 1;
        }

        var posts = await this.GetCachedPostsAsync(cancellationToken);

        int skip = (page - 1) * pageSize;

        var orderedPosts = posts
            .OrderByDescending(p => p.CreatedDate)
            .ToList();

        bool hasMore = orderedPosts.Count > skip + pageSize;
        var resultPosts = orderedPosts
            .Skip(skip)
            .Take(pageSize)
            .ToArray();

        return new PagedPostsResult(resultPosts, hasMore);
    }

    public async Task<Post?> GetPostById(string id, CancellationToken cancellationToken = default)
    {
        var posts = await this.GetCachedPostsAsync(cancellationToken);
        return posts.FirstOrDefault(p => p.Id == id);
    }

    public async IAsyncEnumerable<Post> GetPostsForTag(string tag, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var posts = await this.GetCachedPostsAsync(cancellationToken);

        var results = posts
            .Where(p => p.Tags.Any(t => t.Name.Equals(tag, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(p => p.CreatedDate);

        foreach (var post in results)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return post;
        }
    }

    public async Task<bool> CreatePostAsync(Post post, List<string> tags, CancellationToken cancellationToken = default)
    {
        var result = await this.dataAccess.CreatePostAsync(post, tags, cancellationToken);

        if (result)
        {
            // Invalidate cache to include new post
            this.InvalidateCache();
        }

        return result;
    }

    public async IAsyncEnumerable<Post> GetPostsForDateRange(
        DateTimeOffset startDateTime,
        DateTimeOffset endDateTime,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var posts = await this.GetCachedPostsAsync(cancellationToken);

        var results = posts
            .Where(p => p.CreatedDate >= startDateTime && p.CreatedDate < endDateTime)
            .OrderByDescending(p => p.CreatedDate);

        foreach (var post in results)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return post;
        }
    }

    public async Task<Post?> GetPostForShortTitleAsync(string shortTitle, CancellationToken cancellationToken = default)
    {
        var posts = await this.GetCachedPostsAsync(cancellationToken);
        return posts.FirstOrDefault(p => p.ShortTitle.Equals(shortTitle, StringComparison.OrdinalIgnoreCase));
    }

    public async IAsyncEnumerable<Post> SearchPostsAsync(
        string searchQuery,
        int maxResults = 50,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            yield break;
        }

        var posts = await this.GetCachedPostsAsync(cancellationToken);

        // Simple in-memory search implementation
        var searchTerms = searchQuery.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var scoredResults = posts
            .Select(post => new
            {
                Post = post,
                Score = CalculateSearchScore(post, searchTerms)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(maxResults);

        foreach (var result in scoredResults)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return result.Post;
        }
    }

    /// <summary>
    /// Calculates a simple relevance score for a post based on search terms.
    /// Higher scores indicate better matches.
    /// </summary>
    private static int CalculateSearchScore(Post post, string[] searchTerms)
    {
        int score = 0;

        var title = post.Title?.ToLowerInvariant() ?? string.Empty;
        var linkTitle = post.LinkTitle?.ToLowerInvariant() ?? string.Empty;
        var contents = post.Contents?.ToLowerInvariant() ?? string.Empty;

        foreach (var term in searchTerms)
        {
            // Title matches are worth the most (weight: 10)
            if (title.Contains(term))
            {
                score += 10;
                // Bonus for exact word match
                if (title.Split(' ').Contains(term))
                {
                    score += 5;
                }
            }

            // Link title matches are medium value (weight: 5)
            if (linkTitle.Contains(term))
            {
                score += 5;
            }

            // Content matches are worth less (weight: 1)
            if (contents.Contains(term))
            {
                score += 1;
                // Count multiple occurrences in content (up to 5)
                var occurrences = CountOccurrences(contents, term);
                score += Math.Min(occurrences - 1, 4); // -1 because we already added 1
            }

            // Tag matches (weight: 8)
            if (post.Tags.Any(tag => tag.Name.Contains(term, StringComparison.OrdinalIgnoreCase)))
            {
                score += 8;
            }
        }

        return score;
    }

    private static int CountOccurrences(string text, string term)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(term))
        {
            return 0;
        }

        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(term, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += term.Length;
        }
        return count;
    }

    public async Task<bool> UpdatePostAsync(string id, Post post, List<string> tags, CancellationToken cancellationToken = default)
    {
        var result = await this.dataAccess.UpdatePostAsync(id, post, tags, cancellationToken);

        if (result)
        {
            // Invalidate cache to reflect updates
            this.InvalidateCache();
        }

        return result;
    }

    public async Task<bool> ArchivePostAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await this.dataAccess.ArchivePostAsync(id, cancellationToken);

        if (result)
        {
            // Invalidate cache to reflect archived post
            this.InvalidateCache();
        }

        return result;
    }

    public void Dispose()
    {
        this.cacheLock.Dispose();
    }
}