using System.Text.Json.Serialization;

namespace LinkBlog.Contracts;

public sealed class Post
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("date")]
    public DateTimeOffset Date { get; set; }

    [JsonPropertyName("link")]
    public string? Link { get; set; }

    [JsonPropertyName("linkTitle")]
    public string? LinkTitle { get; set; }

    [JsonPropertyName("contents")]
    public string? Contents { get; set; }

    [JsonPropertyName("tags")]
    public string[] Tags { get; set; }
}
