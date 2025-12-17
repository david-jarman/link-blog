using LinkBlog.Abstractions;
using LinkBlog.Web.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CA1861 // Prefer static readonly arrays in tests for readability

namespace LinkBlog.Web.Tests.Integration;

public class PostStoreDbTests : PostStoreDbTestBase
{
    public PostStoreDbTests(PostgreSqlFixture fixture) : base(fixture)
    {
    }

    // ===== CreatePostAsync Tests =====

    [Fact]
    public async Task CreatePostAsync_WithNewTags_CreatesPostAndTags()
    {
        // Arrange
        var post = new Post
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Test Post",
            ShortTitle = "test-post",
            Contents = "This is a test post",
            CreatedDate = DateTimeOffset.UtcNow,
            LastUpdatedDate = DateTimeOffset.UtcNow
        };
        var tags = new List<string> { "csharp", "testing" };

        // Act
        var result = await this.PostStore.CreatePostAsync(post, tags);

        // Assert
        Assert.True(result);

        // Verify post was created
        var createdPost = await this.PostStore.GetPostById(post.Id);
        Assert.NotNull(createdPost);
        Assert.Equal("Test Post", createdPost.Title);
        Assert.Equal("test-post", createdPost.ShortTitle);
        Assert.Equal(2, createdPost.Tags.Count());
        Assert.Contains(createdPost.Tags, t => t.Name == "csharp");
        Assert.Contains(createdPost.Tags, t => t.Name == "testing");
    }

    [Fact]
    public async Task CreatePostAsync_WithExistingTags_ReusesTagEntities()
    {
        // Arrange
        var firstPost = new Post
        {
            Id = Guid.NewGuid().ToString(),
            Title = "First Post",
            ShortTitle = "first-post",
            Contents = "First post content",
            CreatedDate = DateTimeOffset.UtcNow,
            LastUpdatedDate = DateTimeOffset.UtcNow
        };
        await this.PostStore.CreatePostAsync(firstPost, new List<string> { "csharp" });

        var secondPost = new Post
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Second Post",
            ShortTitle = "second-post",
            Contents = "Second post content",
            CreatedDate = DateTimeOffset.UtcNow,
            LastUpdatedDate = DateTimeOffset.UtcNow
        };

        // Act
        await this.PostStore.CreatePostAsync(secondPost, new List<string> { "csharp", "dotnet" });

        // Assert - Verify tag reuse by checking database directly
        var tagCount = await this.DbContext.Tags.CountAsync(t => t.Name == "csharp");
        Assert.Equal(1, tagCount); // Only one "csharp" tag should exist

        // Verify both posts have the tag
        var postsWithCSharpTag = new List<Post>();
        await foreach (var post in this.PostStore.GetPostsForTag("csharp"))
        {
            postsWithCSharpTag.Add(post);
        }
        Assert.Equal(2, postsWithCSharpTag.Count);
    }

    [Fact]
    public async Task CreatePostAsync_WithDuplicateShortTitle_ThrowsException()
    {
        // Arrange
        var firstPost = new Post
        {
            Id = Guid.NewGuid().ToString(),
            Title = "First Post",
            ShortTitle = "duplicate-title",
            Contents = "First content",
            CreatedDate = DateTimeOffset.UtcNow,
            LastUpdatedDate = DateTimeOffset.UtcNow
        };
        await this.PostStore.CreatePostAsync(firstPost, new List<string>());

        var secondPost = new Post
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Second Post",
            ShortTitle = "duplicate-title", // Same ShortTitle
            Contents = "Second content",
            CreatedDate = DateTimeOffset.UtcNow,
            LastUpdatedDate = DateTimeOffset.UtcNow
        };

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(
            async () => await this.PostStore.CreatePostAsync(secondPost, new List<string>()));
    }

    // ===== GetPosts Tests =====

    [Fact]
    public async Task GetPosts_ReturnsTopNPostsOrderedByDate()
    {
        // Arrange
        var baseDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await CreateTestPostAsync("post-1", "Post 1", date: baseDate.AddDays(1));
        await CreateTestPostAsync("post-2", "Post 2", date: baseDate.AddDays(2));
        await CreateTestPostAsync("post-3", "Post 3", date: baseDate.AddDays(3));
        await CreateTestPostAsync("post-4", "Post 4", date: baseDate.AddDays(4));

        // Act
        var posts = new List<Post>();
        await foreach (var post in this.PostStore.GetPosts(2))
        {
            posts.Add(post);
        }

        // Assert
        Assert.Equal(2, posts.Count);
        Assert.Equal("Post 4", posts[0].Title); // Most recent first
        Assert.Equal("Post 3", posts[1].Title);
    }

    [Fact]
    public async Task GetPosts_ExcludesArchivedPosts()
    {
        // Arrange
        var post1 = await CreateTestPostAsync("active-post", "Active Post");
        var post2 = await CreateTestPostAsync("archived-post", "Archived Post");
        await this.PostStore.ArchivePostAsync(post2);

        // Act
        var posts = new List<Post>();
        await foreach (var post in this.PostStore.GetPosts(10))
        {
            posts.Add(post);
        }

        // Assert
        Assert.Single(posts);
        Assert.Equal("Active Post", posts[0].Title);
    }

    // ===== GetPostsForTag Tests =====

    [Fact]
    public async Task GetPostsForTag_ReturnsOnlyPostsWithTag()
    {
        // Arrange
        await CreateTestPostAsync("csharp-post-1", "CSharp Post 1", tags: new[] { "csharp", "dotnet" });
        await CreateTestPostAsync("csharp-post-2", "CSharp Post 2", tags: new[] { "csharp" });
        await CreateTestPostAsync("java-post", "Java Post", tags: new[] { "java" });

        // Act
        var posts = new List<Post>();
        await foreach (var post in this.PostStore.GetPostsForTag("csharp"))
        {
            posts.Add(post);
        }

        // Assert
        Assert.Equal(2, posts.Count);
        Assert.All(posts, p => Assert.Contains(p.Tags, t => t.Name == "csharp"));
    }

    [Fact]
    public async Task GetPostsForTag_ExcludesArchivedPosts()
    {
        // Arrange
        var post1 = await CreateTestPostAsync("post-1", "Post 1", tags: new[] { "csharp" });
        var post2 = await CreateTestPostAsync("post-2", "Post 2", tags: new[] { "csharp" });
        await this.PostStore.ArchivePostAsync(post2);

        // Act
        var posts = new List<Post>();
        await foreach (var post in this.PostStore.GetPostsForTag("csharp"))
        {
            posts.Add(post);
        }

        // Assert
        Assert.Single(posts);
        Assert.Equal("Post 1", posts[0].Title);
    }

    // ===== GetPostsForDateRange Tests =====

    [Fact]
    public async Task GetPostsForDateRange_ReturnsPostsInRange()
    {
        // Arrange
        var baseDate = new DateTimeOffset(2025, 6, 15, 0, 0, 0, TimeSpan.Zero);
        await CreateTestPostAsync("post-1", "Post 1", date: baseDate.AddDays(-2)); // Before range
        await CreateTestPostAsync("post-2", "Post 2", date: baseDate);              // In range
        await CreateTestPostAsync("post-3", "Post 3", date: baseDate.AddDays(1));   // In range
        await CreateTestPostAsync("post-4", "Post 4", date: baseDate.AddDays(5));   // After range

        // Act
        var posts = new List<Post>();
        await foreach (var post in this.PostStore.GetPostsForDateRange(baseDate, baseDate.AddDays(2)))
        {
            posts.Add(post);
        }

        // Assert
        Assert.Equal(2, posts.Count);
        Assert.Contains(posts, p => p.Title == "Post 2");
        Assert.Contains(posts, p => p.Title == "Post 3");
    }

    [Fact]
    public async Task GetPostsForDateRange_ExcludesArchivedPosts()
    {
        // Arrange
        var baseDate = DateTimeOffset.UtcNow;
        var post1 = await CreateTestPostAsync("post-1", "Post 1", date: baseDate);
        var post2 = await CreateTestPostAsync("post-2", "Post 2", date: baseDate.AddHours(1));
        await this.PostStore.ArchivePostAsync(post1);

        // Act
        var posts = new List<Post>();
        await foreach (var post in this.PostStore.GetPostsForDateRange(
            baseDate.AddDays(-1), baseDate.AddDays(1)))
        {
            posts.Add(post);
        }

        // Assert
        Assert.Single(posts);
        Assert.Equal("Post 2", posts[0].Title);
    }

    // ===== GetPostForShortTitleAsync Tests =====

    [Fact]
    public async Task GetPostForShortTitleAsync_ReturnsCorrectPost()
    {
        // Arrange
        await CreateTestPostAsync("my-test-post", "My Test Post");
        await CreateTestPostAsync("another-post", "Another Post");

        // Act
        var post = await this.PostStore.GetPostForShortTitleAsync("my-test-post");

        // Assert
        Assert.NotNull(post);
        Assert.Equal("My Test Post", post.Title);
        Assert.Equal("my-test-post", post.ShortTitle);
    }

    [Fact]
    public async Task GetPostForShortTitleAsync_ReturnsNullForArchivedPost()
    {
        // Arrange
        var postId = await CreateTestPostAsync("archived-post", "Archived Post");
        await this.PostStore.ArchivePostAsync(postId);

        // Act
        var post = await this.PostStore.GetPostForShortTitleAsync("archived-post");

        // Assert
        Assert.Null(post);
    }

    [Fact]
    public async Task GetPostForShortTitleAsync_ReturnsNullForNonExistentPost()
    {
        // Act
        var post = await this.PostStore.GetPostForShortTitleAsync("non-existent-post");

        // Assert
        Assert.Null(post);
    }

    // ===== SearchPostsAsync Tests (PostgreSQL Full-Text Search) =====

    [Fact]
    public async Task SearchPostsAsync_FindsPostsByTitle()
    {
        // Arrange
        await CreateTestPostAsync("post-1", "Introduction to PostgreSQL", "Content here");
        await CreateTestPostAsync("post-2", "Introduction to MySQL", "Different content");
        await CreateTestPostAsync("post-3", "Advanced C# Features", "No database content");

        // Act
        var results = new List<Post>();
        await foreach (var post in this.PostStore.SearchPostsAsync("PostgreSQL"))
        {
            results.Add(post);
        }

        // Assert
        Assert.Single(results);
        Assert.Equal("Introduction to PostgreSQL", results[0].Title);
    }

    [Fact]
    public async Task SearchPostsAsync_FindsPostsByContents()
    {
        // Arrange
        await CreateTestPostAsync("post-1", "First Post",
            "This post discusses integration testing with PostgreSQL");
        await CreateTestPostAsync("post-2", "Second Post",
            "This post is about Docker containers");

        // Act
        var results = new List<Post>();
        await foreach (var post in this.PostStore.SearchPostsAsync("integration testing"))
        {
            results.Add(post);
        }

        // Assert
        Assert.Single(results);
        Assert.Equal("First Post", results[0].Title);
    }

    [Fact]
    public async Task SearchPostsAsync_FindsMatchesInAllFields()
    {
        // Arrange - Create posts with search term in different fields
        await CreateTestPostAsync("post-1", "Regular Post", contents: "Testcontainers is great", linkTitle: "Link");
        await CreateTestPostAsync("post-2", "Another Post", contents: "Some other content", linkTitle: "Testcontainers Info");
        await CreateTestPostAsync("post-3", "Testcontainers Guide", contents: "Basic content", linkTitle: "Guide");

        // Act - Search for "Testcontainers"
        var results = new List<Post>();
        await foreach (var post in this.PostStore.SearchPostsAsync("Testcontainers"))
        {
            results.Add(post);
        }

        // Assert - All three posts should be found (ranking may vary)
        Assert.Equal(3, results.Count);
        Assert.Contains(results, p => p.Title == "Testcontainers Guide"); // Title match
        Assert.Contains(results, p => p.Title == "Another Post");         // LinkTitle match
        Assert.Contains(results, p => p.Title == "Regular Post");         // Contents match
    }

    [Fact]
    public async Task SearchPostsAsync_RespectsMaxResults()
    {
        // Arrange
        for (int i = 1; i <= 10; i++)
        {
            await CreateTestPostAsync($"post-{i}", $"Testing Post {i}", "Testing content");
        }

        // Act
        var results = new List<Post>();
        await foreach (var post in this.PostStore.SearchPostsAsync("Testing", maxResults: 5))
        {
            results.Add(post);
        }

        // Assert
        Assert.Equal(5, results.Count);
    }

    [Fact]
    public async Task SearchPostsAsync_ReturnsEmptyForEmptyQuery()
    {
        // Arrange
        await CreateTestPostAsync("post-1", "Test Post", "Content");

        // Act
        var results = new List<Post>();
        await foreach (var post in this.PostStore.SearchPostsAsync(""))
        {
            results.Add(post);
        }

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchPostsAsync_ExcludesArchivedPosts()
    {
        // Arrange
        var post1 = await CreateTestPostAsync("post-1", "PostgreSQL Tutorial", "Content");
        await CreateTestPostAsync("post-2", "PostgreSQL Advanced", "Content");
        await this.PostStore.ArchivePostAsync(post1);

        // Act
        var results = new List<Post>();
        await foreach (var post in this.PostStore.SearchPostsAsync("PostgreSQL"))
        {
            results.Add(post);
        }

        // Assert
        Assert.Single(results);
        Assert.Equal("PostgreSQL Advanced", results[0].Title);
    }

    // ===== GetPostById Tests =====

    [Fact]
    public async Task GetPostById_ReturnsCorrectPost()
    {
        // Arrange
        var postId = await CreateTestPostAsync("test-post", "Test Post");

        // Act
        var post = await this.PostStore.GetPostById(postId);

        // Assert
        Assert.NotNull(post);
        Assert.Equal(postId, post.Id);
        Assert.Equal("Test Post", post.Title);
    }

    [Fact]
    public async Task GetPostById_ReturnsNullForNonExistentPost()
    {
        // Act
        var post = await this.PostStore.GetPostById(Guid.NewGuid().ToString());

        // Assert
        Assert.Null(post);
    }

    [Fact]
    public async Task GetPostById_IncludesTags()
    {
        // Arrange
        var postId = await CreateTestPostAsync("post-1", "Post 1", tags: new[] { "csharp", "testing" });

        // Act
        var post = await this.PostStore.GetPostById(postId);

        // Assert
        Assert.NotNull(post);
        Assert.Equal(2, post.Tags.Count());
        Assert.Contains(post.Tags, t => t.Name == "csharp");
        Assert.Contains(post.Tags, t => t.Name == "testing");
    }

    // ===== UpdatePostAsync Tests =====

    [Fact]
    public async Task UpdatePostAsync_UpdatesPostContent()
    {
        // Arrange
        var postId = await CreateTestPostAsync("original-post", "Original Title", "Original content");

        var updatedPost = new Post
        {
            Id = postId,
            Title = "Updated Title",
            ShortTitle = "original-post",
            Contents = "Updated content with new information",
            CreatedDate = DateTimeOffset.UtcNow,
            LastUpdatedDate = DateTimeOffset.UtcNow
        };

        // Act
        var result = await this.PostStore.UpdatePostAsync(postId, updatedPost, new List<string>());

        // Assert
        Assert.True(result);

        var retrieved = await this.PostStore.GetPostById(postId);
        Assert.NotNull(retrieved);
        Assert.Equal("Updated Title", retrieved.Title);
        Assert.Equal("Updated content with new information", retrieved.Contents);
    }

    [Fact]
    public async Task UpdatePostAsync_AddsNewTags()
    {
        // Arrange
        var postId = await CreateTestPostAsync("post-1", "Post 1", tags: new[] { "csharp" });

        var updatedPost = new Post
        {
            Id = postId,
            Title = "Post 1",
            ShortTitle = "post-1",
            Contents = "Content",
            CreatedDate = DateTimeOffset.UtcNow,
            LastUpdatedDate = DateTimeOffset.UtcNow
        };

        // Act - Add "dotnet" and "testing" tags
        await this.PostStore.UpdatePostAsync(postId, updatedPost,
            new List<string> { "csharp", "dotnet", "testing" });

        // Assert
        var retrieved = await this.PostStore.GetPostById(postId);
        Assert.NotNull(retrieved);
        Assert.Equal(3, retrieved.Tags.Count());
        Assert.Contains(retrieved.Tags, t => t.Name == "csharp");
        Assert.Contains(retrieved.Tags, t => t.Name == "dotnet");
        Assert.Contains(retrieved.Tags, t => t.Name == "testing");
    }

    [Fact]
    public async Task UpdatePostAsync_RemovesTags()
    {
        // Arrange
        var postId = await CreateTestPostAsync("post-1", "Post 1",
            tags: new[] { "csharp", "dotnet", "testing" });

        var updatedPost = new Post
        {
            Id = postId,
            Title = "Post 1",
            ShortTitle = "post-1",
            Contents = "Content",
            CreatedDate = DateTimeOffset.UtcNow,
            LastUpdatedDate = DateTimeOffset.UtcNow
        };

        // Act - Keep only "csharp" tag
        await this.PostStore.UpdatePostAsync(postId, updatedPost, new List<string> { "csharp" });

        // Assert
        var retrieved = await this.PostStore.GetPostById(postId);
        Assert.NotNull(retrieved);
        Assert.Single(retrieved.Tags);
        Assert.Equal("csharp", retrieved.Tags.First().Name);
    }

    [Fact]
    public async Task UpdatePostAsync_ReusesExistingTags()
    {
        // Arrange
        await CreateTestPostAsync("post-1", "Post 1", tags: new[] { "csharp" });
        var post2Id = await CreateTestPostAsync("post-2", "Post 2", tags: new[] { "dotnet" });

        var tagCountBefore = await this.DbContext.Tags.CountAsync(t => t.Name == "csharp");
        Assert.Equal(1, tagCountBefore);

        var updatedPost = new Post
        {
            Id = post2Id,
            Title = "Post 2",
            ShortTitle = "post-2",
            Contents = "Content",
            CreatedDate = DateTimeOffset.UtcNow,
            LastUpdatedDate = DateTimeOffset.UtcNow
        };

        // Act - Add existing "csharp" tag to post 2
        await this.PostStore.UpdatePostAsync(post2Id, updatedPost,
            new List<string> { "dotnet", "csharp" });

        // Assert - Should still only be one "csharp" tag entity
        var tagCountAfter = await this.DbContext.Tags.CountAsync(t => t.Name == "csharp");
        Assert.Equal(1, tagCountAfter);
    }

    [Fact]
    public async Task UpdatePostAsync_UpdatesUpdatedDate()
    {
        // Arrange
        var postId = await CreateTestPostAsync("post-1", "Post 1");
        var originalPost = await this.PostStore.GetPostById(postId);
        Assert.NotNull(originalPost);
        var originalUpdatedDate = originalPost.LastUpdatedDate;

        await Task.Delay(100); // Ensure time difference

        var updatedPost = new Post
        {
            Id = postId,
            Title = "Updated Title",
            ShortTitle = "post-1",
            Contents = "Updated content",
            CreatedDate = originalPost.CreatedDate,
            LastUpdatedDate = originalPost.LastUpdatedDate
        };

        // Act
        await this.PostStore.UpdatePostAsync(postId, updatedPost, new List<string>());

        // Assert
        var retrieved = await this.PostStore.GetPostById(postId);
        Assert.NotNull(retrieved);
        Assert.True(retrieved.LastUpdatedDate > originalUpdatedDate);
    }

    [Fact]
    public async Task UpdatePostAsync_ReturnsFalseForNonExistentPost()
    {
        // Arrange
        var post = new Post
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Non-existent",
            ShortTitle = "non-existent",
            Contents = "Content",
            CreatedDate = DateTimeOffset.UtcNow,
            LastUpdatedDate = DateTimeOffset.UtcNow
        };

        // Act
        var result = await this.PostStore.UpdatePostAsync(post.Id, post, new List<string>());

        // Assert
        Assert.False(result);
    }

    // ===== ArchivePostAsync Tests =====

    [Fact]
    public async Task ArchivePostAsync_SetsIsArchivedFlag()
    {
        // Arrange
        var postId = await CreateTestPostAsync("post-1", "Post 1");

        // Act
        var result = await this.PostStore.ArchivePostAsync(postId);

        // Assert
        Assert.True(result);

        var archived = await this.PostStore.GetPostById(postId);
        Assert.NotNull(archived);
        Assert.True(archived.IsArchived);
    }

    [Fact]
    public async Task ArchivePostAsync_UpdatesUpdatedDate()
    {
        // Arrange
        var postId = await CreateTestPostAsync("post-1", "Post 1");
        var originalPost = await this.PostStore.GetPostById(postId);
        Assert.NotNull(originalPost);
        var originalUpdatedDate = originalPost.LastUpdatedDate;

        await Task.Delay(100); // Ensure time difference

        // Act
        await this.PostStore.ArchivePostAsync(postId);

        // Assert
        var archived = await this.PostStore.GetPostById(postId);
        Assert.NotNull(archived);
        Assert.True(archived.LastUpdatedDate > originalUpdatedDate);
    }

    [Fact]
    public async Task ArchivePostAsync_ReturnsFalseForNonExistentPost()
    {
        // Act
        var result = await this.PostStore.ArchivePostAsync(Guid.NewGuid().ToString());

        // Assert
        Assert.False(result);
    }

    // ===== Helper Methods =====

    private async Task<string> CreateTestPostAsync(
        string shortTitle,
        string title,
        string? contents = null,
        DateTimeOffset? date = null,
        string? linkTitle = null,
        IEnumerable<string>? tags = null)
    {
        var post = new Post
        {
            Id = Guid.NewGuid().ToString(),
            Title = title,
            ShortTitle = shortTitle,
            Contents = contents ?? "Default test content",
            CreatedDate = date ?? DateTimeOffset.UtcNow,
            LastUpdatedDate = date ?? DateTimeOffset.UtcNow,
            LinkTitle = linkTitle
        };

        await this.PostStore.CreatePostAsync(post, tags?.ToList() ?? new List<string>());
        return post.Id;
    }
}