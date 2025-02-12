using System.Text.Json;
using LinkBlog.Contracts;

namespace LinkBlog.ApiService;

public interface IPostStore
{
    IEnumerable<Post?> GetPosts();
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
}
