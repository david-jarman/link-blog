using System.Text.Json.Serialization;

namespace LinkBlog.Contracts;

public sealed class Post
{
    public string Id { get; set; }

    public string Title { get; set; }

    public DateTimeOffset Date { get; set; }

    public string? Link { get; set; }

    public string? LinkTitle { get; set; }

    public string? Contents { get; set; }

    public List<Tag> Tags { get; set; } = new();
}
