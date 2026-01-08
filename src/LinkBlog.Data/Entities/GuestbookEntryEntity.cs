using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LinkBlog.Data;

[Table("GuestbookEntries")]
[Index(nameof(CreatedDate))]
[Index(nameof(IsApproved))]
public sealed class GuestbookEntryEntity
{
    [Required]
    public string Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Message { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(200)]
    public string? Website { get; set; }

    [Required]
    public DateTimeOffset CreatedDate { get; set; }

    public bool IsApproved { get; set; }

    public GuestbookEntryEntity()
    {
        Id = Guid.NewGuid().ToString();
        Name = string.Empty;
        Message = string.Empty;
        Email = null;
        Website = null;
        CreatedDate = DateTimeOffset.UtcNow;
        IsApproved = false;
    }
}
