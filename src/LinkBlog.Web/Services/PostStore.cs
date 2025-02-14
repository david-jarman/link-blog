using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LinkBlog.Contracts;

namespace LinkBlog.Web.Services;

public interface IPostStore
{
    IAsyncEnumerable<Post> GetPosts(CancellationToken cancellationToken = default);

    IAsyncEnumerable<Post> GetPostsForTag(string tag, CancellationToken cancellationToken = default);
}

public class PostStoreDb : IPostStore
{
    private readonly PostDbContext postDbContext;

    public PostStoreDb(PostDbContext postDbContext)
    {
        this.postDbContext = postDbContext;
    }

    public async IAsyncEnumerable<Post> GetPosts([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
         await foreach (var post in this.postDbContext.Posts.AsAsyncEnumerable())
         {
            cancellationToken.ThrowIfCancellationRequested();

            yield return post;
         }
    }

    public async IAsyncEnumerable<Post> GetPostsForTag(string tag, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var post in this.postDbContext.Posts.AsAsyncEnumerable())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (post.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                yield return post;
            }
        }
    }
}

public class StaticPostStore : IPostStore
{
    private static readonly ActivitySource _activitySource = new("LinkBlog.ApiService");

    private readonly ILogger<StaticPostStore> _logger;
    private readonly DirectoryInfo _directory;

    public StaticPostStore(ILogger<StaticPostStore> logger)
    {
        _logger = logger;
        _directory = new DirectoryInfo("Data");
    }

    public IAsyncEnumerable<Post> GetPosts(CancellationToken cancellationToken = default)
    {
        return GetAllPosts(cancellationToken);
    }

    public async IAsyncEnumerable<Post> GetPostsForTag(string tag, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity(nameof(GetPostsForTag));
        activity?.SetTag("tag", tag);
        cancellationToken.Register(() => activity?.AddEvent(new ActivityEvent("Cancelled")));

        await foreach (var post in GetAllPosts(cancellationToken))
        {
            if (post.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                yield return post;
            }
        }
    }

    private async IAsyncEnumerable<Post> GetAllPosts([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        this._logger.LogDebug("Getting posts from {Directory}", _directory.FullName);

        foreach (var file in _directory.GetFiles("*.json"))
        {
            Post? post = null;
            using var fileStream = file.OpenRead();
            try
            {
                post = await JsonSerializer.DeserializeAsync<Post>(fileStream, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (JsonException ex)
            {
                this._logger.LogError(ex, "Failed to deserialize post from {File}", file.FullName);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to read post from {File}. Exception: {ExceptionMessage}", file.FullName, ex.Message);
            }

            if (post != null)
            {
                yield return post;
            }
            else
            {
                this._logger.LogWarning("Post object was null after attempting to deserialize file {File}", file.FullName);
            }
        }
    }
}
