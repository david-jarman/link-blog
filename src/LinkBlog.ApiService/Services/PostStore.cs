using System.Runtime.CompilerServices;
using System.Text.Json;
using LinkBlog.Contracts;

namespace LinkBlog.ApiService;

public interface IPostStore
{
    IEnumerable<Post?> GetPosts();

    IAsyncEnumerable<Post> GetPostsForTag(string tag, CancellationToken cancellationToken);
}

public class StaticPostStore : IPostStore
{
    private readonly ILogger<StaticPostStore> _logger;
    private readonly DirectoryInfo _directory;

    public StaticPostStore(ILogger<StaticPostStore> logger)
    {
        _logger = logger;
        _directory = new DirectoryInfo("Data");
    }

    public IEnumerable<Post?> GetPosts()
    {
        this._logger.LogDebug("Getting posts from {Directory}", _directory.FullName);

        return _directory.GetFiles("*.json")
            .Select(file => JsonSerializer.Deserialize<Post>(File.ReadAllText(file.FullName)));
    }

    public async IAsyncEnumerable<Post> GetPostsForTag(string tag, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
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
