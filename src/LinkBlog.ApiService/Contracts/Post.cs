namespace LinkBlog.ApiService;

public sealed class Post
{
    public Post(
        string id,
        string title,
        DateTimeOffset date,
        string? url = null,
        string? contents = null,
        IReadOnlySet<string>? tags = null)
    {
        Id = id;
        Title = title;
        Date = date;
        Url = url;
        Contents = contents;
        Tags = tags ?? new HashSet<string>();
    }

    public string Id { get; }
    public string Title { get; }
    public DateTimeOffset Date { get; }
    public string? Url { get; }
    public string? Contents { get; }
    public IReadOnlySet<string> Tags { get; }
}
