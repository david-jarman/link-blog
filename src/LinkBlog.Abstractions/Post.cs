namespace LinkBlog.Abstractions;

public sealed class Post
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string ShortTitle { get; set; } = string.Empty;

    public DateTimeOffset CreatedDate { get; set; }

    public string? Link { get; set; }

    public string? LinkTitle { get; set; }

    public string Contents { get; set; } = string.Empty;

    // Chose IEnumerable over List because otherwise we can get into
    // an infinite loop resolving tags -> posts -> tags etc when calling
    // ToList() in the extensions to convert between contracts.
    public IEnumerable<Tag> Tags { get; set; } = Enumerable.Empty<Tag>();
}