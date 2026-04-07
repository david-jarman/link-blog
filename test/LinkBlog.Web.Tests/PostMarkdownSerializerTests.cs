using LinkBlog.Abstractions;
using LinkBlog.Data;

namespace LinkBlog.Web.Tests;

public class PostMarkdownSerializerTests
{
    private readonly PostMarkdownSerializer sut = new();

    private const string FullPostMarkdown = """
        ---
        id: abc-123
        title: My Test Post
        short-title: my-test-post
        type: post
        created: 2025-03-15T10:00:00-08:00
        updated: 2025-03-15T12:00:00-08:00
        link: https://example.com/article
        link-title: Example Article
        tags:
        - tech
        - dotnet
        archived: false
        ---

        Hello **world**! This is a [link](https://example.com).
        """;

    private const string MinimalPostMarkdown = """
        ---
        id: def-456
        title: Minimal Post
        short-title: minimal-post
        type: post
        created: 2025-06-01T08:00:00-07:00
        updated: 2025-06-01T08:00:00-07:00
        tags: []
        archived: false
        ---

        Just some text.
        """;

    [Fact]
    public void Deserialize_FullPost_MapsAllFields()
    {
        var post = sut.Deserialize(FullPostMarkdown);

        Assert.Equal("abc-123", post.Id);
        Assert.Equal("My Test Post", post.Title);
        Assert.Equal("my-test-post", post.ShortTitle);
        Assert.Equal("post", post.Type);
        Assert.Equal(new DateTimeOffset(2025, 3, 15, 10, 0, 0, TimeSpan.FromHours(-8)), post.CreatedDate);
        Assert.Equal(new DateTimeOffset(2025, 3, 15, 12, 0, 0, TimeSpan.FromHours(-8)), post.LastUpdatedDate);
        Assert.Equal("https://example.com/article", post.Link);
        Assert.Equal("Example Article", post.LinkTitle);
        Assert.False(post.IsArchived);
        Assert.Collection(post.Tags,
            t => Assert.Equal("tech", t.Name),
            t => Assert.Equal("dotnet", t.Name));
    }

    [Fact]
    public void Deserialize_RendersMarkdownBodyToHtml()
    {
        var post = sut.Deserialize(FullPostMarkdown);

        Assert.Contains("<strong>world</strong>", post.Contents);
        Assert.Contains("<a href=\"https://example.com\">link</a>", post.Contents);
    }

    [Fact]
    public void Deserialize_StoresRawMarkdownInMarkdownSource()
    {
        var post = sut.Deserialize(FullPostMarkdown);

        Assert.Contains("Hello **world**!", post.MarkdownSource);
    }

    [Fact]
    public void Deserialize_MinimalPost_OmitsOptionalFields()
    {
        var post = sut.Deserialize(MinimalPostMarkdown);

        Assert.Equal("def-456", post.Id);
        Assert.Null(post.Link);
        Assert.Null(post.LinkTitle);
        Assert.Empty(post.Tags);
    }

    [Fact]
    public void Deserialize_MissingType_DefaultsToPost()
    {
        var markdown = """
            ---
            id: xyz-789
            title: No Type Post
            short-title: no-type-post
            created: 2025-01-01T00:00:00+00:00
            updated: 2025-01-01T00:00:00+00:00
            tags: []
            archived: false
            ---

            Body.
            """;

        var post = sut.Deserialize(markdown);

        Assert.Equal("post", post.Type);
    }

    [Fact]
    public void Deserialize_DoesNotRenderRawHtml()
    {
        var markdown = """
            ---
            id: html-test
            title: HTML Test
            short-title: html-test
            created: 2025-01-01T00:00:00+00:00
            updated: 2025-01-01T00:00:00+00:00
            tags: []
            archived: false
            ---

            <script>alert('xss')</script>
            """;

        var post = sut.Deserialize(markdown);

        Assert.DoesNotContain("<script>", post.Contents);
    }

    [Fact]
    public void Serialize_ProducesFrontmatterAndBody()
    {
        var post = new Post
        {
            Id = "abc-123",
            Title = "My Test Post",
            ShortTitle = "my-test-post",
            Type = "post",
            CreatedDate = new DateTimeOffset(2025, 3, 15, 10, 0, 0, TimeSpan.FromHours(-8)),
            LastUpdatedDate = new DateTimeOffset(2025, 3, 15, 12, 0, 0, TimeSpan.FromHours(-8)),
            Link = "https://example.com/article",
            LinkTitle = "Example Article",
            MarkdownSource = "Hello **world**!",
            IsArchived = false,
            Tags = new[] { new Tag { Id = "tech", Name = "tech" }, new Tag { Id = "dotnet", Name = "dotnet" } }
        };

        var result = sut.Serialize(post);

        Assert.StartsWith("---\n", result);
        Assert.Contains("id: abc-123", result);
        Assert.Contains("title: My Test Post", result);
        Assert.Contains("short-title: my-test-post", result);
        Assert.Contains("type: post", result);
        Assert.Contains("link: https://example.com/article", result);
        Assert.Contains("link-title: Example Article", result);
        Assert.Contains("Hello **world**!", result);
    }

    [Fact]
    public void Serialize_OmitsNullOptionalFields()
    {
        var post = new Post
        {
            Id = "def-456",
            Title = "Minimal",
            ShortTitle = "minimal",
            Type = "post",
            CreatedDate = DateTimeOffset.UtcNow,
            LastUpdatedDate = DateTimeOffset.UtcNow,
            MarkdownSource = "Body.",
            Tags = Enumerable.Empty<Tag>()
        };

        var result = sut.Serialize(post);

        Assert.DoesNotContain("link:", result);
        Assert.DoesNotContain("link-title:", result);
    }

    [Fact]
    public void GetBlobName_ReturnsDateAndShortTitle()
    {
        var post = new Post
        {
            ShortTitle = "my-test-post",
            // 2025-03-15T18:00:00Z = 2025-03-15T10:00:00-08:00 (Pacific)
            CreatedDate = new DateTimeOffset(2025, 3, 15, 18, 0, 0, TimeSpan.Zero)
        };

        var blobName = PostMarkdownSerializer.GetBlobName(post);

        Assert.Equal("2025-03-15-my-test-post.md", blobName);
    }
}