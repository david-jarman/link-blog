using System.Globalization;
using LinkBlog.Abstractions;

namespace LinkBlog.IntegrationTests.Infrastructure;

/// <summary>
/// Factory for creating test data objects.
/// </summary>
public static class TestDataFactory
{
    private static int _postCounter;

    /// <summary>
    /// Creates a test post with unique content.
    /// </summary>
    public static Post CreateTestPost(
        string? title = null,
        string? shortTitle = null,
        DateTimeOffset? date = null,
        string? contents = null,
        string? link = null,
        string? linkTitle = null,
        bool isArchived = false)
    {
        var counter = Interlocked.Increment(ref _postCounter);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

        return new Post
        {
            Id = Guid.NewGuid().ToString(),
            Title = title ?? $"Test Post {counter}",
            ShortTitle = shortTitle ?? $"test-post-{counter}-{timestamp}",
            CreatedDate = date ?? DateTimeOffset.UtcNow,
            LastUpdatedDate = date ?? DateTimeOffset.UtcNow,
            Contents = contents ?? $"This is test post content {counter}. It contains some searchable text.",
            Link = link,
            LinkTitle = linkTitle,
            IsArchived = isArchived
        };
    }

    /// <summary>
    /// Creates a test post with specific tags for testing tag-based queries.
    /// </summary>
    public static (Post post, List<string> tags) CreateTestPostWithTags(
        List<string> tags,
        string? title = null,
        string? contents = null)
    {
        var post = CreateTestPost(title: title, contents: contents);
        return (post, tags);
    }

    /// <summary>
    /// Creates a test post with a specific date for archive testing.
    /// </summary>
    public static Post CreateTestPostForDate(
        int year,
        int month,
        int day,
        string? title = null)
    {
        var date = new DateTimeOffset(year, month, day, 12, 0, 0, TimeSpan.FromHours(-8));
        return CreateTestPost(title: title, date: date);
    }

    /// <summary>
    /// Creates a test post with searchable content.
    /// </summary>
    public static Post CreateSearchablePost(
        string searchTerm,
        string location = "contents")
    {
        var counter = Interlocked.Increment(ref _postCounter);

        return location.ToLower(CultureInfo.InvariantCulture) switch
        {
            "title" => CreateTestPost(
                title: $"Test Post About {searchTerm}",
                contents: $"Generic content {counter}"),

            "link" => CreateTestPost(
                title: $"Test Post {counter}",
                linkTitle: $"Link About {searchTerm}",
                link: "https://example.com",
                contents: $"Generic content {counter}"),

            _ => CreateTestPost(
                title: $"Test Post {counter}",
                contents: $"This post contains information about {searchTerm} and other topics.")
        };
    }

    /// <summary>
    /// Creates a small test image as a byte array (1x1 PNG).
    /// </summary>
    public static byte[] CreateTestImage()
    {
        // 1x1 transparent PNG (smallest valid PNG)
        return new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR chunk
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, // 1x1 dimensions
            0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
            0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, // IDAT chunk
            0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
            0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, // Image data
            0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, // IEND chunk
            0x42, 0x60, 0x82
        };
    }
}
