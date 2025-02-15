namespace LinkBlog.Contracts;

public sealed class Tag
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<Post> Posts { get; set; } = new();
}