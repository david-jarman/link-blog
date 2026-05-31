using LinkBlog.Abstractions;

namespace LinkBlog.Data;

/// <summary>
/// Slim interface for data access operations needed by the cached post store.
/// </summary>
public interface IPostDataAccess
{
    IAsyncEnumerable<Post> GetAllPostsAsync(CancellationToken cancellationToken = default);
}