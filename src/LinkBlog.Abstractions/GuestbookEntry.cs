namespace LinkBlog.Abstractions;

public sealed class GuestbookEntry
{
    private readonly TimeZoneInfo pacificZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Website { get; set; }

    public DateTimeOffset CreatedDate { get; set; }

    public bool IsApproved { get; set; }

    public DateTimeOffset LocalCreatedTime => TimeZoneInfo.ConvertTime(CreatedDate, pacificZone);
}
