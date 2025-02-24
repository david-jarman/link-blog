using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LinkBlog.Data;

[Table("Posts")]
[Index(nameof(ShortTitle), IsUnique = true)]
[Index(nameof(Date))]
internal sealed class PostEntity
{
    [Required]
    public string Id { get; set; }

    [Required]
    public string Title { get; set; }

    [Required]
    public string ShortTitle { get; set; }

    [Required]
    public DateTimeOffset Date { get; set; }

    public string? Link { get; set; }

    public string? LinkTitle { get; set; }

    [Required]
    public string Contents { get; set; }

    public List<TagEntity> Tags { get; set; } = new();

    public PostEntity()
    {
        Id = Guid.NewGuid().ToString();
        Title = string.Empty;
        ShortTitle = string.Empty;
        Date = DateTimeOffset.UtcNow;
        Link = null;
        LinkTitle = null;
        Contents = string.Empty;
    }
}
