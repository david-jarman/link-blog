namespace LinkBlog.Abstractions;

public sealed class Tag
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public IEnumerable<Post> Posts { get; init; } = Enumerable.Empty<Post>();
}