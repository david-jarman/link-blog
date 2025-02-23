using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkBlog.Data;

[Table("Tags")]
internal sealed class TagEntity
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    public List<PostEntity> Posts { get; set; } = new();
}