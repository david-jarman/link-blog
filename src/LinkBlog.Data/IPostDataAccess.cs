using LinkBlog.Abstractions;

namespace LinkBlog.Data;

/// <summary>
/// Slim interface for data access operations needed by the cached post store.
/// </summary>
public interface IPostDataAccess
{
    IAsyncEnumerable<Post> GetAllPostsAsync(CancellationToken cancellationToken = default);
    Task<bool> CreatePostAsync(Post post, List<string> tags, CancellationToken cancellationToken = default);
    Task<bool> UpdatePostAsync(string id, Post post, List<string> tags, CancellationToken cancellationToken = default);
    Task<bool> ArchivePostAsync(Post post, CancellationToken cancellationToken = default);
}