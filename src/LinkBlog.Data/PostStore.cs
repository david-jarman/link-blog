using LinkBlog.Abstractions;

namespace LinkBlog.Data;

public record PagedPostsResult(Post[] Posts, bool HasMore);

public interface IPostStore
{
    IAsyncEnumerable<Post> GetPosts(int topN, CancellationToken cancellationToken = default);

    Task<PagedPostsResult> GetPostsPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<Post?> GetPostById(string id, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Post> GetPostsForTag(string tag, CancellationToken cancellationToken = default);

    Task<bool> CreatePostAsync(Post post, List<string> tags, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Post> GetPostsForDateRange(DateTimeOffset startDateTime, DateTimeOffset endDateTime, CancellationToken cancellationToken = default);

    Task<Post?> GetPostForShortTitleAsync(string shortTitle, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Post> SearchPostsAsync(string searchQuery, int maxResults = 50, CancellationToken cancellationToken = default);

    Task<bool> UpdatePostAsync(string id, Post post, List<string> tags, CancellationToken cancellationToken = default);

    Task<bool> ArchivePostAsync(string id, CancellationToken cancellationToken = default);
}