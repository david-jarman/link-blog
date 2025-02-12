using System.Text.Json.Serialization;

namespace LinkBlog.ApiService;

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
