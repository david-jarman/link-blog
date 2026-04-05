namespace LinkBlog.Abstractions;

public sealed class Post
{
    private readonly TimeZoneInfo pacificZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string ShortTitle { get; set; } = string.Empty;

    /// <summary>Content type. Defaults to "post". Reserved for future types (note, fyi, etc.).</summary>
    public string Type { get; set; } = "post";

    public DateTimeOffset CreatedDate { get; set; }

    public DateTimeOffset LastUpdatedDate { get; set; }

    public string? Link { get; set; }

    public string? LinkTitle { get; set; }

    /// <summary>Rendered HTML content, populated when loading from Blob Storage.</summary>
    public string Contents { get; set; } = string.Empty;

    /// <summary>Raw markdown source, used by admin editor and serialized to the blob file body.</summary>
    public string MarkdownSource { get; set; } = string.Empty;

    public bool IsArchived { get; set; }

    public IEnumerable<Tag> Tags { get; set; } = Enumerable.Empty<Tag>();

    public DateTimeOffset LocalCreatedTime => TimeZoneInfo.ConvertTime(CreatedDate, pacificZone);

    public string UrlPath => $"/archive/{LocalCreatedTime.Year}/{LocalCreatedTime:MM}/{LocalCreatedTime:dd}/{ShortTitle}";
}