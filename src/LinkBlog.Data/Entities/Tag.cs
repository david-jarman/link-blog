using System.ComponentModel.DataAnnotations;

namespace LinkBlog.Data;

public sealed class Tag
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    public List<PostEntity> Posts { get; set; } = new();
}