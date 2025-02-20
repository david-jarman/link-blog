using System.ComponentModel.DataAnnotations;

namespace LinkBlog.Contracts;

public sealed class Tag
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    public List<Post> Posts { get; set; } = new();
}