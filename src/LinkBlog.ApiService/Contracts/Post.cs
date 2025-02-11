using System.Text.Json.Serialization;

namespace LinkBlog.ApiService;

public sealed class Post
{
    [JsonConstructor]
    public Post(
        string id,
        string title,
        DateTimeOffset date,
        string? url = null,
        string? contents = null,
        string[]? tags = null)
    {
        Id = id;
        Title = title;
        Date = date;
        Url = url;
        Contents = contents;
        Tags = tags ?? Array.Empty<string>();
    }

    [JsonPropertyName("id")]
    public string Id { get; }

    [JsonPropertyName("title")]
    public string Title { get; }

    [JsonPropertyName("date")]
    public DateTimeOffset Date { get; }

    [JsonPropertyName("url")]
    public string? Url { get; }

    [JsonPropertyName("contents")]
    public string? Contents { get; }

    [JsonPropertyName("tags")]
    public string[] Tags { get; }
}
