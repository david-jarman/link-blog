# In-Memory Post Cache Prototype

## Overview

This prototype implements an in-memory caching layer for blog posts with the following features:
- **All posts cached in memory** using `IMemoryCache`
- **Periodic cache refresh** every 5 minutes via background service
- **Cache invalidation** on create/update/archive operations
- **In-memory search** using simple string matching instead of PostgreSQL full-text search
- **Feature flag** to toggle between cached and direct database access

## Implementation

### 1. CachedPostStore (`src/LinkBlog.Data/CachedPostStore.cs`)

A decorator implementation of `IPostStore` that wraps `PostStoreDb`:

**Key Features:**
- **Thread-safe caching** using `SemaphoreSlim` for cache refresh operations
- **Cache-aside pattern**: Check cache → if miss → load from DB → populate cache
- **Automatic invalidation**: Cache is cleared on any write operation (create/update/archive)
- **Memory-efficient**: Stores all posts in a single `List<Post>` cached object

**Read Operations (Cached):**
- `GetPosts(topN)` - Returns top N posts ordered by date
- `GetPostById(id)` - Single post lookup by ID
- `GetPostsForTag(tag)` - Filter by tag name
- `GetPostsForDateRange()` - Filter by date range
- `GetPostForShortTitleAsync()` - Lookup by URL slug
- `SearchPostsAsync()` - In-memory search with relevance scoring

**Write Operations (Cache Invalidation):**
- `CreatePostAsync()` - Creates post in DB, then invalidates cache
- `UpdatePostAsync()` - Updates post in DB, then invalidates cache
- `ArchivePostAsync()` - Archives post in DB, then invalidates cache

### 2. PostCacheRefreshService (`src/LinkBlog.Data/PostCacheRefreshService.cs`)

A background service (`IHostedService`) that periodically refreshes the cache:

**Behavior:**
- Runs on application startup (initial warm-up)
- Refreshes every 5 minutes by default
- Creates a new service scope to resolve scoped `PostStoreDb`
- Continues running even if individual refreshes fail
- Logs all operations for monitoring

### 3. In-Memory Search Algorithm

Replaces PostgreSQL full-text search with simple string matching:

**Search Scoring (Higher = Better Match):**
- Title match: 10 points (exact word: +5 bonus)
- Tag match: 8 points
- Link title match: 5 points
- Content match: 1 point per occurrence (max +5)

**Implementation:**
- Tokenizes search query by spaces
- Searches across all cached posts
- Orders results by relevance score
- Returns top N results

### 4. Configuration

**Feature Flag** (`appsettings.Development.json`):
```json
{
  "PostStore": {
    "EnableInMemoryCache": true
  }
}
```

**Service Registration** (`src/LinkBlog.Data/Extensions/ServiceCollectionExtensions.cs`):
- When `enableInMemoryCache = true`:
  - Registers `IMemoryCache` with size limit of 100 items
  - Registers `PostStoreDb` as itself
  - Registers `CachedPostStore` as `IPostStore` implementation
  - Registers `PostCacheRefreshService` as hosted service
- When `false`: Original behavior (direct `PostStoreDb` registration)

## How to Test

### 1. Build and Run
```bash
dotnet build
dotnet run --project src/LinkBlog.Web/LinkBlog.Web.csproj
```

### 2. Monitor Logs

Look for these log messages:
```
Post cache refresh service starting. Refresh interval: 00:05:00
Refreshing post cache from database...
Post cache refreshed with X posts
```

### 3. Test Read Performance

**Without Cache:**
- Set `EnableInMemoryCache: false` in appsettings
- Visit home page multiple times
- Check database query logs

**With Cache:**
- Set `EnableInMemoryCache: true` in appsettings
- Visit home page multiple times
- After initial load, subsequent requests should not hit database
- Check logs for cache hits

### 4. Test Search

**Compare search results:**
1. Search with PostgreSQL FTS (cache disabled)
2. Search with in-memory algorithm (cache enabled)
3. Compare relevance ordering and results

**Search Test Queries:**
- Single word: "blazor"
- Multiple words: "blog post"
- Partial matches: "data"

### 5. Test Cache Invalidation

1. Create a new blog post via admin interface
2. Check logs for "Post cache invalidated"
3. Verify new post appears immediately on home page
4. Check logs for "Refreshing post cache from database..."

### 6. Test Cache Refresh

1. Wait 5 minutes after startup
2. Check logs for periodic refresh message
3. Verify background service continues running

## Evaluation Criteria

### ✅ Pros

1. **Performance Gains:**
   - Eliminates database queries for read operations (99% of traffic)
   - Faster search (no PostgreSQL query overhead)
   - Reduced database load and connection pool usage
   - Sub-millisecond response times for cached reads

2. **Simplicity:**
   - No complex PostgreSQL-specific indexes or queries
   - Portable search algorithm (works with any database)
   - Easy to understand and debug
   - No dependency on PostgreSQL full-text search extensions

3. **Scalability:**
   - Scales with memory (not database connections)
   - Can handle high read throughput
   - Background refresh doesn't block requests
   - Suitable for read-heavy workloads

4. **Developer Experience:**
   - Feature flag allows easy A/B testing
   - Existing code doesn't change (implements same interface)
   - Can be deployed gradually with feature flag

### ❌ Cons

1. **Memory Usage:**
   - All posts stored in memory (estimate: ~1-10 MB for 1000 posts)
   - Not suitable if you have millions of posts
   - Multiple application instances = multiple caches

2. **Eventual Consistency:**
   - 5-minute cache refresh means updates aren't immediately visible
   - Mitigated by cache invalidation on writes
   - But changes from external sources (direct DB updates) won't be visible until refresh

3. **Search Quality:**
   - Simple string matching vs. stemming/lemmatization
   - No language-specific features (plurals, synonyms)
   - No stop word filtering
   - Relevance scoring is basic compared to PostgreSQL FTS

4. **Cold Start:**
   - Initial request after restart loads all posts
   - First search might be slower (but only once)
   - Background service mitigates this with startup warm-up

5. **Multi-Instance Challenges:**
   - Each application instance has its own cache
   - Cache invalidation only affects local instance
   - Not suitable for multi-server deployments without distributed cache

## Recommendations

### ✅ Use In-Memory Cache If:
- You have < 10,000 blog posts (manageable in memory)
- Blog is read-heavy (typical for blogs: 99%+ reads)
- Running on single instance or using sticky sessions
- Simple search is acceptable (most blog searches are simple)
- Want to reduce database costs and improve performance

### ❌ Don't Use If:
- You have millions of posts
- Running on multiple instances without shared cache
- Need advanced full-text search features (stemming, etc.)
- Database queries are already fast enough
- Write operations are frequent

## Next Steps

### If You Like This Approach:

1. **Benchmark Performance:**
   - Use tools like `wrk` or `Apache Bench` to measure response times
   - Compare cached vs. non-cached under load
   - Measure memory usage

2. **Improve Search:**
   - Add stemming library (e.g., Porter Stemmer)
   - Implement stop word filtering
   - Add fuzzy matching (Levenshtein distance)
   - Consider tf-idf scoring

3. **Distributed Cache:**
   - Replace `IMemoryCache` with `IDistributedCache` (Redis)
   - Enables multi-instance deployments
   - Adds complexity but better scalability

4. **Monitoring:**
   - Add metrics for cache hit/miss ratio
   - Monitor cache refresh duration
   - Alert on cache refresh failures

5. **Tune Cache Refresh:**
   - Make refresh interval configurable
   - Add manual cache refresh endpoint for admins
   - Consider smart refresh (only when DB changed)

### Alternative Approaches:

1. **Hybrid Approach:**
   - Use in-memory cache for home page (top N posts)
   - Use database for search (keep PostgreSQL FTS)
   - Best of both worlds

2. **Output Caching Only:**
   - Keep existing architecture
   - Increase output cache duration (current: 60s for home)
   - Simpler, but less flexible

3. **Read Replicas:**
   - Add PostgreSQL read replica
   - Route read queries to replica
   - Better scalability without caching complexity

## Files Changed

- `src/LinkBlog.Data/CachedPostStore.cs` - New file
- `src/LinkBlog.Data/PostCacheRefreshService.cs` - New file
- `src/LinkBlog.Data/Extensions/ServiceCollectionExtensions.cs` - Modified
- `src/LinkBlog.Web/Program.cs` - Modified
- `src/LinkBlog.Web/appsettings.Development.json` - Modified

## Configuration Reference

**Enable Cache:**
```json
{
  "PostStore": {
    "EnableInMemoryCache": true
  }
}
```

**Disable Cache (Default):**
```json
{
  "PostStore": {
    "EnableInMemoryCache": false
  }
}
```

Or simply omit the configuration section - defaults to `false`.

## Performance Expectations

Based on typical blog workloads:

| Metric | Without Cache | With Cache |
|--------|--------------|------------|
| Home page (10 posts) | ~20-50ms | ~1-5ms |
| Search query | ~50-200ms | ~5-20ms |
| Tag filter | ~20-50ms | ~1-5ms |
| Single post lookup | ~10-20ms | ~1-2ms |
| Database queries | Every request | Only writes + 5min refresh |
| Memory usage | ~50MB | ~60MB (+10MB cache) |

**Note:** Actual performance depends on database size, server specs, and network latency.

## Conclusion

This prototype demonstrates a viable approach for caching blog posts in memory. It's particularly well-suited for:
- Small to medium-sized blogs (< 10,000 posts)
- Read-heavy workloads
- Single-instance deployments
- Performance-sensitive applications

The feature flag approach allows you to test this in production with minimal risk, and you can easily revert to the database-backed implementation if needed.

For production use, consider:
1. Benchmarking performance improvements
2. Monitoring memory usage and cache effectiveness
3. Improving search algorithm if needed
4. Planning for multi-instance scaling (distributed cache)
