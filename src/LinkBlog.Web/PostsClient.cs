using System.Text.Json.Serialization;

namespace LinkBlog.Web;

public class PostsClient(HttpClient httpClient)
{
    public async Task<Post[]> GetPostsAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        List<Post>? posts = null;

        await foreach (var post in httpClient.GetFromJsonAsAsyncEnumerable<Post>("/api/posts", cancellationToken))
        {
            if (posts?.Count >= maxItems)
            {
                break;
            }
            if (post is not null)
            {
                posts ??= [];
                posts.Add(post);
            }
        }

        return posts?.ToArray() ?? [];
    }
}

public sealed class Post
{
    [JsonConstructor]
    public Post(
        string id,
        string title,
        DateTimeOffset date,
        string? link = null,
        string? linkTitle = null,
        string? contents = null,
        string[]? tags = null)
    {
        Id = id;
        Title = title;
        Date = date;
        Link = link;
        LinkTitle = linkTitle;
        Contents = contents;
        Tags = tags ?? Array.Empty<string>();
    }

    [JsonPropertyName("id")]
    public string Id { get; }

    [JsonPropertyName("title")]
    public string Title { get; }

    [JsonPropertyName("date")]
    public DateTimeOffset Date { get; }

    [JsonPropertyName("link")]
    public string? Link { get; }

    [JsonPropertyName("linkTitle")]
    public string? LinkTitle { get; }

    [JsonPropertyName("contents")]
    public string? Contents { get; }

    [JsonPropertyName("tags")]
    public string[] Tags { get; }
}
