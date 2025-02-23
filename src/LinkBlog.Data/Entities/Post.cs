using System.ComponentModel.DataAnnotations;

namespace LinkBlog.Data;

public sealed class Post
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

    public List<Tag> Tags { get; set; } = new();

    public Post()
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
