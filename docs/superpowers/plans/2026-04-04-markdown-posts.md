# Markdown-Based Post Storage Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the PostgreSQL database with Azure Blob Storage-backed markdown files as the post content store, using Markdig to render markdown to HTML.

**Architecture:** `MarkdownPostDataAccess` (new) implements `IPostDataAccess` by reading/writing `.md` files to Azure Blob Storage. `PostMarkdownSerializer` handles parsing YAML frontmatter and rendering markdown via Markdig. `CachedPostStore` is unchanged; it still sits on top of `IPostDataAccess` for in-memory caching. The admin page switches from Trix to EasyMDE.

**Tech Stack:** Markdig (markdown→HTML), YamlDotNet (YAML frontmatter), Azure Blob Storage SDK (already wired), EasyMDE (markdown editor, self-hosted), xunit + Moq (tests).

**Spec:** `docs/superpowers/specs/2026-04-04-markdown-posts-design.md`

---

## File Map

**Create:**
- `src/LinkBlog.Data/PostMarkdownSerializer.cs` — pure serialization: markdown file ↔ `Post` object
- `src/LinkBlog.Data/MarkdownPostDataAccess.cs` — `IPostDataAccess` backed by Azure Blob
- `test/LinkBlog.Web.Tests/PostMarkdownSerializerTests.cs` — unit tests for serializer
- `tools/LinkBlog.Migration/LinkBlog.Migration.csproj`
- `tools/LinkBlog.Migration/Program.cs`

**Modify:**
- `src/LinkBlog.Abstractions/Post.cs` — add `Type`, `MarkdownSource`; change `Tags` to `set`
- `src/LinkBlog.Data/IPostDataAccess.cs` — `ArchivePostAsync` takes `Post` not `string id`
- `src/LinkBlog.Data/PostStore.cs` — update `PostDataAccess` (EF) to match new interface; remove it in Task 9
- `src/LinkBlog.Data/CachedPostStore.cs` — `ArchivePostAsync` looks up post from cache before delegating
- `src/LinkBlog.Data/Extensions/ServiceCollectionExtensions.cs` — replace EF Core wiring with Blob wiring
- `src/LinkBlog.Data/LinkBlog.Data.csproj` — remove EF Core packages; add Markdig, YamlDotNet
- `src/LinkBlog.Web/Program.cs` — call new `AddPostStore()`, remove Postgres connection string logic
- `src/LinkBlog.Web/LinkBlog.Web.csproj` — remove EF Core packages
- `src/LinkBlog.AppHost/Program.cs` — remove Postgres and MigrationService
- `src/LinkBlog.AppHost/LinkBlog.AppHost.csproj` — remove Postgres hosting package and MigrationService ref
- `src/LinkBlog.Web/Components/Pages/Admin/AdminHome.razor` — replace Trix with EasyMDE
- `test/LinkBlog.Web.Tests/LinkBlog.Web.Tests.csproj` — remove Postgres/EF test packages
- `Directory.Packages.props` — add Markdig, YamlDotNet; remove EF Core and Postgres packages

**Delete:**
- `src/LinkBlog.Data/PostDbContext.cs`
- `src/LinkBlog.Data/Entities/PostEntity.cs`
- `src/LinkBlog.Data/Entities/TagEntity.cs`
- `src/LinkBlog.Data/Extensions/EntityExtensions.cs`
- `src/LinkBlog.Data/Migrations/` (all files)
- `src/LinkBlog.Data/Scripts/migrate-idempotent.sql`
- `src/LinkBlog.MigrationService/` (entire project)
- `test/LinkBlog.Web.Tests/Integration/PostStoreDbTests.cs`
- `test/LinkBlog.Web.Tests/Integration/PostStoreDbTestBase.cs`
- `test/LinkBlog.Web.Tests/Fixtures/PostgreSqlCollectionFixture.cs`
- `test/LinkBlog.Web.Tests/Fixtures/PostgreSqlFixture.cs`
- `test/LinkBlog.Web.Tests/PendingDatabaseMigrationTests.cs`
- `src/LinkBlog.Web/wwwroot/js/trix.js` (and `.LICENSE.txt`)
- `src/LinkBlog.Web/wwwroot/js/trix-extensions.js`
- `src/LinkBlog.Web/wwwroot/js/upload-attachments.js`
- `src/LinkBlog.Web/wwwroot/js/draft-manager.js`
- `src/LinkBlog.Web/wwwroot/css/trix.css`
- `src/LinkBlog.Web/wwwroot/css/draft-manager.css`

---

## Task 1: Update `Post` Model

**Files:**
- Modify: `src/LinkBlog.Abstractions/Post.cs`

- [ ] **Step 1: Update `Post.cs`**

Replace the file content:

```csharp
namespace LinkBlog.Abstractions;

public sealed class Post
{
    private readonly TimeZoneInfo pacificZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string ShortTitle { get; set; } = string.Empty;

    /// <summary>Content type. Defaults to "post". Reserved for future types (note, fyi, etc.).</summary>
    public string Type { get; set; } = "post";

    public DateTimeOffset CreatedDate { get; set; }

    public DateTimeOffset LastUpdatedDate { get; set; }

    public string? Link { get; set; }

    public string? LinkTitle { get; set; }

    /// <summary>Rendered HTML content, populated when loading from Blob Storage.</summary>
    public string Contents { get; set; } = string.Empty;

    /// <summary>Raw markdown source, used by admin editor and serialized to the blob file body.</summary>
    public string MarkdownSource { get; set; } = string.Empty;

    public bool IsArchived { get; set; }

    public IEnumerable<Tag> Tags { get; set; } = Enumerable.Empty<Tag>();

    public DateTimeOffset LocalCreatedTime => TimeZoneInfo.ConvertTime(CreatedDate, pacificZone);

    public string UrlPath => $"/archive/{LocalCreatedTime.Year}/{LocalCreatedTime:MM}/{LocalCreatedTime:dd}/{ShortTitle}";
}
```

- [ ] **Step 2: Build to verify no compilation errors**

```bash
dotnet build
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add src/LinkBlog.Abstractions/Post.cs
git commit -m "feat: add Type and MarkdownSource to Post model"
```

---

## Task 2: Update `IPostDataAccess` — `ArchivePostAsync` Takes `Post`

This change propagates the blob-addressable `Post` object down to `MarkdownPostDataAccess` so it can determine the blob name when archiving.

**Files:**
- Modify: `src/LinkBlog.Data/IPostDataAccess.cs`
- Modify: `src/LinkBlog.Data/PostStore.cs` (EF Core `PostDataAccess` class)
- Modify: `src/LinkBlog.Data/CachedPostStore.cs`

- [ ] **Step 1: Update `IPostDataAccess.cs`**

```csharp
using LinkBlog.Abstractions;

namespace LinkBlog.Data;

/// <summary>
/// Slim interface for data access operations needed by the cached post store.
/// </summary>
public interface IPostDataAccess
{
    IAsyncEnumerable<Post> GetAllPostsAsync(CancellationToken cancellationToken = default);
    Task<bool> CreatePostAsync(Post post, List<string> tags, CancellationToken cancellationToken = default);
    Task<bool> UpdatePostAsync(string id, Post post, List<string> tags, CancellationToken cancellationToken = default);
    Task<bool> ArchivePostAsync(Post post, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Update `PostDataAccess.ArchivePostAsync` in `PostStore.cs`**

Find the existing `ArchivePostAsync` method in `PostDataAccess` (around line 130) and replace its signature and body:

```csharp
public async Task<bool> ArchivePostAsync(Post post, CancellationToken cancellationToken = default)
{
    var postEntity = await this.postDbContext.Posts
        .FirstOrDefaultAsync(p => p.Id == post.Id, cancellationToken);

    if (postEntity == null)
    {
        return false;
    }

    postEntity.IsArchived = true;
    postEntity.UpdatedDate = DateTimeOffset.UtcNow;

    await this.postDbContext.SaveChangesAsync(cancellationToken);
    return true;
}
```

- [ ] **Step 3: Update `CachedPostStore.ArchivePostAsync` in `CachedPostStore.cs`**

Replace the existing `ArchivePostAsync` method body to look up the post from cache first:

```csharp
public async Task<bool> ArchivePostAsync(string id, CancellationToken cancellationToken = default)
{
    var posts = await this.GetCachedPostsAsync(cancellationToken);
    var post = posts.FirstOrDefault(p => p.Id == id);
    if (post == null)
    {
        return false;
    }

    var result = await this.dataAccess.ArchivePostAsync(post, cancellationToken);

    if (result)
    {
        this.InvalidateCache();
    }

    return result;
}
```

- [ ] **Step 4: Build to verify no compilation errors**

```bash
dotnet build
```

Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add src/LinkBlog.Data/IPostDataAccess.cs src/LinkBlog.Data/PostStore.cs src/LinkBlog.Data/CachedPostStore.cs
git commit -m "feat: ArchivePostAsync takes Post object for blob-addressable archive"
```

---

## Task 3: Add Markdig and YamlDotNet Packages

**Files:**
- Modify: `Directory.Packages.props`
- Modify: `src/LinkBlog.Data/LinkBlog.Data.csproj`
- Modify: `test/LinkBlog.Web.Tests/LinkBlog.Web.Tests.csproj`

- [ ] **Step 1: Add package versions to `Directory.Packages.props`**

Add inside the `<ItemGroup>` block, after the existing Aspire entries:

```xml
<PackageVersion Include="Markdig" Version="0.41.1" />
<PackageVersion Include="YamlDotNet" Version="16.3.0" />
```

- [ ] **Step 2: Add packages to `LinkBlog.Data.csproj`**

Add inside the existing `<ItemGroup>` that has `PackageReference` elements:

```xml
<PackageReference Include="Markdig" />
<PackageReference Include="YamlDotNet" />
```

- [ ] **Step 3: Add YamlDotNet to test project for serializer tests**

In `test/LinkBlog.Web.Tests/LinkBlog.Web.Tests.csproj`, add inside the `PackageReference` `<ItemGroup>`:

```xml
<PackageReference Include="YamlDotNet" />
```

- [ ] **Step 4: Restore packages**

```bash
dotnet restore
```

Expected: Restore succeeded.

- [ ] **Step 5: Commit**

```bash
git add Directory.Packages.props src/LinkBlog.Data/LinkBlog.Data.csproj test/LinkBlog.Web.Tests/LinkBlog.Web.Tests.csproj
git commit -m "chore: add Markdig and YamlDotNet packages"
```

---

## Task 4: Create `PostMarkdownSerializer` with Tests (TDD)

**Files:**
- Create: `test/LinkBlog.Web.Tests/PostMarkdownSerializerTests.cs`
- Create: `src/LinkBlog.Data/PostMarkdownSerializer.cs`

- [ ] **Step 1: Write the failing tests**

Create `test/LinkBlog.Web.Tests/PostMarkdownSerializerTests.cs`:

```csharp
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
            // 2025-03-15T10:00:00-08:00 is 2025-03-15 in Pacific time
            CreatedDate = new DateTimeOffset(2025, 3, 15, 18, 0, 0, TimeSpan.Zero)
        };

        var blobName = sut.GetBlobName(post);

        Assert.Equal("2025-03-15-my-test-post.md", blobName);
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```bash
dotnet test test/LinkBlog.Web.Tests/LinkBlog.Web.Tests.csproj --filter "PostMarkdownSerializerTests"
```

Expected: Compilation error — `PostMarkdownSerializer` does not exist yet.

- [ ] **Step 3: Create `PostMarkdownSerializer.cs`**

Create `src/LinkBlog.Data/PostMarkdownSerializer.cs`:

```csharp
using System.Runtime.CompilerServices;
using LinkBlog.Abstractions;
using Markdig;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LinkBlog.Data;

public sealed class PostMarkdownSerializer
{
    private static readonly TimeZoneInfo PacificZone =
        TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

    private readonly MarkdownPipeline pipeline;
    private readonly IDeserializer yamlDeserializer;
    private readonly ISerializer yamlSerializer;

    public PostMarkdownSerializer()
    {
        pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .DisableHtml()
            .Build();

        yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();
    }

    public Post Deserialize(string fileContent)
    {
        var (frontmatterYaml, markdownBody) = SplitFrontmatter(fileContent);
        var fm = yamlDeserializer.Deserialize<PostFrontmatter>(frontmatterYaml);
        var renderedHtml = Markdown.ToHtml(markdownBody, pipeline);

        return new Post
        {
            Id = fm.Id,
            Title = fm.Title,
            ShortTitle = fm.ShortTitle,
            Type = fm.Type ?? "post",
            CreatedDate = fm.Created,
            LastUpdatedDate = fm.Updated,
            Link = fm.Link,
            LinkTitle = fm.LinkTitle,
            Contents = renderedHtml,
            MarkdownSource = markdownBody,
            IsArchived = fm.Archived,
            Tags = (fm.Tags ?? []).Select(t => new Tag { Id = t, Name = t })
        };
    }

    public string Serialize(Post post)
    {
        var fm = new PostFrontmatter
        {
            Id = post.Id,
            Title = post.Title,
            ShortTitle = post.ShortTitle,
            Type = post.Type,
            Created = post.CreatedDate,
            Updated = DateTimeOffset.UtcNow,
            Link = post.Link,
            LinkTitle = post.LinkTitle,
            Tags = post.Tags.Select(t => t.Name).ToList(),
            Archived = post.IsArchived
        };

        var yaml = yamlSerializer.Serialize(fm);
        return $"---\n{yaml}---\n\n{post.MarkdownSource}";
    }

    public string GetBlobName(Post post)
    {
        var local = TimeZoneInfo.ConvertTime(post.CreatedDate, PacificZone);
        return $"{local:yyyy-MM-dd}-{post.ShortTitle}.md";
    }

    private static (string frontmatter, string body) SplitFrontmatter(string content)
    {
        if (!content.StartsWith("---\n"))
        {
            return (string.Empty, content);
        }

        var closeIndex = content.IndexOf("\n---", 4, StringComparison.Ordinal);
        if (closeIndex < 0)
        {
            return (string.Empty, content);
        }

        var frontmatter = content[4..closeIndex];
        var afterClose = closeIndex + 4; // skip \n---
        var body = afterClose < content.Length && content[afterClose] == '\n'
            ? content[(afterClose + 1)..].TrimStart('\n')
            : content[afterClose..].TrimStart('\n');

        return (frontmatter, body);
    }
}

internal sealed class PostFrontmatter
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ShortTitle { get; set; } = string.Empty;
    public string? Type { get; set; }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset Updated { get; set; }
    public string? Link { get; set; }
    public string? LinkTitle { get; set; }
    public List<string>? Tags { get; set; }
    public bool Archived { get; set; }
}
```

- [ ] **Step 4: Run tests to confirm they pass**

```bash
dotnet test test/LinkBlog.Web.Tests/LinkBlog.Web.Tests.csproj --filter "PostMarkdownSerializerTests"
```

Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/LinkBlog.Data/PostMarkdownSerializer.cs test/LinkBlog.Web.Tests/PostMarkdownSerializerTests.cs
git commit -m "feat: add PostMarkdownSerializer with tests"
```

---

## Task 5: Create `MarkdownPostDataAccess`

**Files:**
- Create: `src/LinkBlog.Data/MarkdownPostDataAccess.cs`

- [ ] **Step 1: Create `MarkdownPostDataAccess.cs`**

Create `src/LinkBlog.Data/MarkdownPostDataAccess.cs`:

```csharp
using System.Runtime.CompilerServices;
using Azure.Storage.Blobs;
using LinkBlog.Abstractions;

namespace LinkBlog.Data;

/// <summary>
/// Azure Blob Storage-backed implementation of IPostDataAccess.
/// Each post is a markdown file with YAML frontmatter stored in the "posts" blob container.
/// </summary>
public sealed class MarkdownPostDataAccess : IPostDataAccess
{
    private const string ContainerName = "posts";
    private readonly BlobContainerClient containerClient;
    private readonly PostMarkdownSerializer serializer;

    public MarkdownPostDataAccess(BlobServiceClient blobServiceClient, PostMarkdownSerializer serializer)
    {
        this.containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
        this.serializer = serializer;
    }

    public async IAsyncEnumerable<Post> GetAllPostsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await this.containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        await foreach (var blobItem in this.containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var blobClient = this.containerClient.GetBlobClient(blobItem.Name);
            var response = await blobClient.DownloadContentAsync(cancellationToken);
            var content = response.Value.Content.ToString();

            yield return this.serializer.Deserialize(content);
        }
    }

    public async Task<bool> CreatePostAsync(Post post, List<string> tags, CancellationToken cancellationToken = default)
    {
        await this.containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        post.Tags = tags.Select(t => new Tag { Id = t, Name = t });
        var blobName = this.serializer.GetBlobName(post);
        var content = this.serializer.Serialize(post);
        var blobClient = this.containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(BinaryData.FromString(content), overwrite: false, cancellationToken);
        return true;
    }

    public async Task<bool> UpdatePostAsync(string id, Post post, List<string> tags, CancellationToken cancellationToken = default)
    {
        post.Tags = tags.Select(t => new Tag { Id = t, Name = t });
        post.LastUpdatedDate = DateTimeOffset.UtcNow;

        var blobName = this.serializer.GetBlobName(post);
        var content = this.serializer.Serialize(post);
        var blobClient = this.containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(BinaryData.FromString(content), overwrite: true, cancellationToken);
        return true;
    }

    public async Task<bool> ArchivePostAsync(Post post, CancellationToken cancellationToken = default)
    {
        post.IsArchived = true;
        var blobName = this.serializer.GetBlobName(post);
        var content = this.serializer.Serialize(post);
        var blobClient = this.containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(BinaryData.FromString(content), overwrite: true, cancellationToken);
        return true;
    }
}
```

- [ ] **Step 2: Build to verify no compilation errors**

```bash
dotnet build
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add src/LinkBlog.Data/MarkdownPostDataAccess.cs
git commit -m "feat: add MarkdownPostDataAccess backed by Azure Blob Storage"
```

---

## Task 6: Rewire Dependency Injection

**Files:**
- Modify: `src/LinkBlog.Data/Extensions/ServiceCollectionExtensions.cs`
- Modify: `src/LinkBlog.Web/Program.cs`

- [ ] **Step 1: Replace `ServiceCollectionExtensions.cs`**

Overwrite `src/LinkBlog.Data/Extensions/ServiceCollectionExtensions.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LinkBlog.Data.Extensions;

public static class ServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddPostStore(this IHostApplicationBuilder app)
    {
        app.Services.AddMemoryCache(options =>
        {
            options.SizeLimit = 100;
        });

        app.Services.AddSingleton<PostMarkdownSerializer>();
        app.Services.AddScoped<IPostDataAccess, MarkdownPostDataAccess>();
        app.Services.AddScoped<IPostStore, CachedPostStore>();
        app.Services.AddHostedService<PostCacheRefreshService>();

        return app;
    }
}
```

- [ ] **Step 2: Update `Web/Program.cs`**

Replace the `AddPostStore` call and the Heroku Postgres connection string block. Find this section:

```csharp
bool isHeroku = !string.IsNullOrEmpty(config["DYNO"]);
builder.AddPostStore("postgresdb", options =>
{
    if (isHeroku)
    {
        var match = Regex.Match(config["DATABASE_URL"] ?? "", @"postgres://(.*):(.*)@(.*):(.*)/(.*)");
        options.ConnectionString = $"Server={match.Groups[3]};Port={match.Groups[4]};User Id={match.Groups[1]};Password={match.Groups[2]};Database={match.Groups[5]};sslmode=Prefer;Trust Server Certificate=true";
    }
});
```

Replace it with:

```csharp
bool isHeroku = !string.IsNullOrEmpty(config["DYNO"]);
builder.AddPostStore();
```

Also remove the `using System.Text.RegularExpressions;` import at the top of `Program.cs` if it's now unused (check first — it may be used elsewhere).

- [ ] **Step 3: Build to verify no compilation errors**

```bash
dotnet build
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add src/LinkBlog.Data/Extensions/ServiceCollectionExtensions.cs src/LinkBlog.Web/Program.cs
git commit -m "feat: rewire DI to use Blob-backed MarkdownPostDataAccess"
```

---

## Task 7: Update `AppHost` — Remove Postgres and MigrationService

**Files:**
- Modify: `src/LinkBlog.AppHost/Program.cs`
- Modify: `src/LinkBlog.AppHost/LinkBlog.AppHost.csproj`

- [ ] **Step 1: Replace `AppHost/Program.cs`**

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(azurite =>
    {
        azurite.WithDataVolume()
            .WithLifetime(ContainerLifetime.Persistent)
            .WithBlobPort(34553);
    });

var blobStore = storage.AddBlobs("blobstore");

builder.AddProject<Projects.LinkBlog_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(blobStore)
    .WaitFor(blobStore);

builder.Build().Run();
```

- [ ] **Step 2: Update `AppHost/LinkBlog.AppHost.csproj`**

Remove the `Aspire.Hosting.PostgreSQL` package reference and the `LinkBlog.MigrationService` project reference:

```xml
<Project Sdk="Aspire.AppHost.Sdk/13.2.1">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Description>Entrypoint for local development.</Description>
    <OutputType>Exe</OutputType>
    <UserSecretsId>dd48d512-4144-4234-897b-a2df95788389</UserSecretsId>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\LinkBlog.Web\LinkBlog.Web.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.Azure.Storage" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Build to verify no compilation errors**

```bash
dotnet build
```

Expected: Build succeeded (MigrationService still exists on disk, just no longer referenced).

- [ ] **Step 4: Commit**

```bash
git add src/LinkBlog.AppHost/Program.cs src/LinkBlog.AppHost/LinkBlog.AppHost.csproj
git commit -m "chore: remove Postgres and MigrationService from AppHost"
```

---

## Task 8: Delete EF Core Infrastructure

**Files:** Multiple deletions across `LinkBlog.Data`, `LinkBlog.MigrationService`, `LinkBlog.Web`

- [ ] **Step 1: Remove EF Core packages from `LinkBlog.Data.csproj`**

Edit `src/LinkBlog.Data/LinkBlog.Data.csproj` to remove EF Core package references. The file should look like:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Description>Data access layer with markdown serialization and blob storage.</Description>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\LinkBlog.Abstractions\LinkBlog.Abstractions.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.Azure.Storage.Blobs" />
    <PackageReference Include="Markdig" />
    <PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" />
    <PackageReference Include="YamlDotNet" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Remove EF Core packages from `LinkBlog.Web.csproj`**

Edit `src/LinkBlog.Web/LinkBlog.Web.csproj` to remove EF Core references. The file should look like:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Description>Blazor SSR that serves the link blog website.</Description>
    <UserSecretsId>6acfaab6-53be-4e80-a2f0-ef9b0a7688d6</UserSecretsId>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\LinkBlog.ServiceDefaults\LinkBlog.ServiceDefaults.csproj" />
    <ProjectReference Include="..\LinkBlog.Data\LinkBlog.Data.csproj" />
    <ProjectReference Include="..\LinkBlog.Feed\LinkBlog.Feed.csproj" />
    <ProjectReference Include="..\LinkBlog.Images\LinkBlog.Images.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.Azure.Storage.Blobs" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Remove EF Core-related package versions from `Directory.Packages.props`**

Remove these lines:

```xml
<PackageVersion Include="Aspire.Hosting.PostgreSQL" Version="13.2.1" />
<PackageVersion Include="Aspire.Npgsql.EntityFrameworkCore.PostgreSQL" Version="13.2.1" />
<PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.5" />
<PackageVersion Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.5" />
<PackageVersion Include="Testcontainers.PostgreSql" Version="4.11.0" />
```

- [ ] **Step 4: Delete EF Core source files from `LinkBlog.Data`**

```bash
rm src/LinkBlog.Data/PostDbContext.cs
rm src/LinkBlog.Data/Entities/PostEntity.cs
rm src/LinkBlog.Data/Entities/TagEntity.cs
rm src/LinkBlog.Data/Extensions/EntityExtensions.cs
rm -rf src/LinkBlog.Data/Migrations
rm src/LinkBlog.Data/Scripts/migrate-idempotent.sql
rmdir src/LinkBlog.Data/Entities
rmdir src/LinkBlog.Data/Scripts
```

- [ ] **Step 5: Remove `PostDataAccess` class from `PostStore.cs`**

Edit `src/LinkBlog.Data/PostStore.cs` and delete everything from the `/// <summary>` comment above `public class PostDataAccess` through the final closing `}` of that class. The file should only contain `PagedPostsResult` and `IPostStore`:

```csharp
using System.Runtime.CompilerServices;
using LinkBlog.Abstractions;

namespace LinkBlog.Data;

public record PagedPostsResult(Post[] Posts, bool HasMore);

public interface IPostStore
{
    IAsyncEnumerable<Post> GetPosts(int topN, CancellationToken cancellationToken = default);

    Task<PagedPostsResult> GetPostsPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<Post?> GetPostById(string id, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Post> GetPostsForTag(string tag, CancellationToken cancellationToken = default);

    Task<bool> CreatePostAsync(Post post, List<string> tags, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Post> GetPostsForDateRange(DateTimeOffset startDateTime, DateTimeOffset endDateTime, CancellationToken cancellationToken = default);

    Task<Post?> GetPostForShortTitleAsync(string shortTitle, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Post> SearchPostsAsync(string searchQuery, int maxResults = 50, CancellationToken cancellationToken = default);

    Task<bool> UpdatePostAsync(string id, Post post, List<string> tags, CancellationToken cancellationToken = default);

    Task<bool> ArchivePostAsync(string id, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 6: Delete `LinkBlog.MigrationService` project**

```bash
rm -rf src/LinkBlog.MigrationService
```

Also remove the project from `LinkBlog.slnx` — open the solution file and delete the line referencing `LinkBlog.MigrationService`.

- [ ] **Step 7: Build to verify no compilation errors**

```bash
dotnet build
```

Expected: Build succeeded.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "chore: remove EF Core and Postgres infrastructure"
```

---

## Task 9: Update Test Project — Remove Postgres Tests

**Files:**
- Delete: `test/LinkBlog.Web.Tests/Integration/PostStoreDbTests.cs`
- Delete: `test/LinkBlog.Web.Tests/Integration/PostStoreDbTestBase.cs`
- Delete: `test/LinkBlog.Web.Tests/Fixtures/PostgreSqlCollectionFixture.cs`
- Delete: `test/LinkBlog.Web.Tests/Fixtures/PostgreSqlFixture.cs`
- Delete: `test/LinkBlog.Web.Tests/PendingDatabaseMigrationTests.cs`
- Modify: `test/LinkBlog.Web.Tests/LinkBlog.Web.Tests.csproj`

- [ ] **Step 1: Delete Postgres-specific test files**

```bash
rm test/LinkBlog.Web.Tests/Integration/PostStoreDbTests.cs
rm test/LinkBlog.Web.Tests/Integration/PostStoreDbTestBase.cs
rm test/LinkBlog.Web.Tests/Fixtures/PostgreSqlCollectionFixture.cs
rm test/LinkBlog.Web.Tests/Fixtures/PostgreSqlFixture.cs
rm test/LinkBlog.Web.Tests/PendingDatabaseMigrationTests.cs
rmdir test/LinkBlog.Web.Tests/Integration
rmdir test/LinkBlog.Web.Tests/Fixtures
```

- [ ] **Step 2: Update `LinkBlog.Web.Tests.csproj`**

Remove the Postgres/EF Core test packages. The file should look like:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="bunit" />
    <PackageReference Include="coverlet.collector" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="Moq" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="YamlDotNet" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\LinkBlog.Web\LinkBlog.Web.csproj" />
    <ProjectReference Include="..\..\src\LinkBlog.Abstractions\LinkBlog.Abstractions.csproj" />
    <ProjectReference Include="..\..\src\LinkBlog.Data\LinkBlog.Data.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Run all tests to verify they pass**

```bash
dotnet test
```

Expected: All tests pass (including `PostMarkdownSerializerTests` and `FormValidationTests`).

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "chore: remove Postgres integration tests"
```

---

## Task 10: Update Admin Page — Replace Trix with EasyMDE

**Files:**
- Modify: `src/LinkBlog.Web/Components/Pages/Admin/AdminHome.razor`
- Modify: `src/LinkBlog.Web/Components/Pages/Admin/AdminHome.razor.css`
- Add: `src/LinkBlog.Web/wwwroot/js/easymde.min.js`
- Add: `src/LinkBlog.Web/wwwroot/css/easymde.min.css`
- Delete: `src/LinkBlog.Web/wwwroot/js/trix.js` + `.LICENSE.txt`
- Delete: `src/LinkBlog.Web/wwwroot/js/trix-extensions.js`
- Delete: `src/LinkBlog.Web/wwwroot/js/upload-attachments.js`
- Delete: `src/LinkBlog.Web/wwwroot/js/draft-manager.js`
- Delete: `src/LinkBlog.Web/wwwroot/css/trix.css`
- Delete: `src/LinkBlog.Web/wwwroot/css/draft-manager.css`

- [ ] **Step 1: Download EasyMDE assets**

Download the latest EasyMDE release from https://github.com/Ionaru/easy-markdown-editor/releases. Get `easymde.min.js` and `easymde.min.css` and place them at:
- `src/LinkBlog.Web/wwwroot/js/easymde.min.js`
- `src/LinkBlog.Web/wwwroot/css/easymde.min.css`

- [ ] **Step 2: Delete Trix assets**

```bash
rm src/LinkBlog.Web/wwwroot/js/trix.js
rm src/LinkBlog.Web/wwwroot/js/trix.js.LICENSE.txt
rm src/LinkBlog.Web/wwwroot/js/trix-extensions.js
rm src/LinkBlog.Web/wwwroot/js/upload-attachments.js
rm src/LinkBlog.Web/wwwroot/js/draft-manager.js
rm src/LinkBlog.Web/wwwroot/css/trix.css
rm src/LinkBlog.Web/wwwroot/css/draft-manager.css
```

- [ ] **Step 3: Replace `AdminHome.razor`**

The key changes from the current version:
1. Remove the `@using Microsoft.EntityFrameworkCore` using
2. Replace `HeadContent` (remove Trix/draft scripts, add EasyMDE + init script)
3. Remove draft manager markup
4. Replace `<trix-editor>` with `InputTextArea`; bind to `PostForm.Contents` (now markdown)
5. When loading an existing post for editing, bind to `post.MarkdownSource` instead of `post.Contents`
6. In `HandleSubmitAsync`, set `post.MarkdownSource = PostForm.Contents`; remove `DbUpdateException` catches
7. Remove `EnableDrafts` field and logic

Write the updated file:

```razor
@page "/admin"
@page "/admin/{id}"

@using LinkBlog.Abstractions
@using LinkBlog.Data
@using Microsoft.AspNetCore.Authorization
@using System.ComponentModel.DataAnnotations
@attribute [Authorize(Policy = "Admin")]

@inject IPostStore PostStore
@inject ILogger<AdminHome> Logger

<HeadContent>
    <link rel="stylesheet" type="text/css" href="/css/easymde.min.css">
    <script type="text/javascript" src="/js/easymde.min.js"></script>
    <script type="text/javascript">
        document.addEventListener('DOMContentLoaded', function () {
            var easyMDE = new EasyMDE({
                element: document.getElementById("PostContent"),
                spellChecker: false,
                autosave: {
                    enabled: true,
                    uniqueId: "admin-post-@(Id ?? "new")",
                    delay: 1000,
                }
            });
        });
    </script>
</HeadContent>

<PageTitle>Admin</PageTitle>

<h1>Admin page</h1>

@if (!string.IsNullOrWhiteSpace(ErrorMessage))
{
    <div class="alert alert-danger" role="alert">
        Error: @ErrorMessage
    </div>
}

@if (!string.IsNullOrWhiteSpace(SuccessMessage))
{
    <div class="alert alert-success" role="alert">
        @SuccessMessage
    </div>
}

<EditForm Model="PostForm" FormName="postForm" OnValidSubmit="HandleSubmitAsync">
    <DataAnnotationsValidator />

    <div class="form-group">
        <label for="PostTitle" class="form-label">Title: </label>
        <InputText id="PostTitle" @bind-Value="PostForm!.Title" class="form-control" />
        <ValidationMessage For="@(() => PostForm.Title)" class="validation-message" />
    </div>

    <div class="form-group">
        <label for="ShortTitle" class="form-label">Short Title: </label>
        <InputText id="ShortTitle" @bind-Value="PostForm.ShortTitle" class="form-control" />
        <ValidationMessage For="@(() => PostForm.ShortTitle)" class="validation-message" />
        <small class="hint">Must be unique, URL-friendly identifier for the post.</small>
    </div>

    <div class="form-group">
        <label for="PostContent" class="form-label">Post content: </label>
        <InputTextArea id="PostContent" @bind-Value="PostForm.Contents" class="post-editor" />
        <ValidationMessage For="@(() => PostForm.Contents)" class="validation-message" />
    </div>

    <div class="form-group">
        <label for="PostLink" class="form-label">Link: </label>
        <InputText id="PostLink" @bind-Value="PostForm.Link" class="form-control" />
        <ValidationMessage For="@(() => PostForm.Link)" class="validation-message" />
    </div>

    <div class="form-group">
        <label for="LinkTitle" class="form-label">Link Title:</label>
        <InputText id="LinkTitle" @bind-Value="PostForm.LinkTitle" class="form-control" />
        <ValidationMessage For="@(() => PostForm.LinkTitle)" class="validation-message" />
    </div>

    <div class="form-group">
        <label for="PostTags" class="form-label">Tags (comma separated):</label>
        <InputText id="PostTags" @bind-Value="PostForm.Tags" class="form-control" />
        <ValidationMessage For="@(() => PostForm.Tags)" class="validation-message" />
    </div>

    <div class="form-group">
        <button type="submit" class="btn-primary">Submit</button>
        @if (!string.IsNullOrWhiteSpace(Id))
        {
            <button type="submit"
                    class="btn-danger mt-2"
                    formaction="/admin/@Id/archive"
                    formmethod="post"
                    onclick="return confirm('Are you sure you want to archive this post? It will no longer appear on the site.')">
                Archive Post
            </button>
        }
    </div>
</EditForm>

@code {
    [Parameter]
    public string? Id { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? Message { get; set; }

    [SupplyParameterFromForm]
    private PostFormModel? PostForm { get; set; }

    private string? ErrorMessage { get; set; }

    private string? SuccessMessage { get; set; }

    public class PostFormModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title must be less than 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Short title is required")]
        [StringLength(100, ErrorMessage = "Short title must be less than 100 characters")]
        [RegularExpression(@"^[a-z0-9\-]+$", ErrorMessage = "Short title must contain only lowercase letters, numbers, and hyphens")]
        public string ShortTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Content is required")]
        public string Contents { get; set; } = string.Empty;

        public string? Link { get; set; }

        public string? LinkTitle { get; set; }

        [Required(ErrorMessage = "Tags are required")]
        [RegularExpression(@"^([a-z0-9\-]+)(,[a-z0-9\-]+)*$", ErrorMessage = "Tags must be a comma-separated list and only contain lowercase letters, numbers, and hyphens")]
        public string Tags { get; set; } = string.Empty;
    }

    protected override async Task OnInitializedAsync()
    {
        if (!string.IsNullOrWhiteSpace(Message))
        {
            SuccessMessage = Message;
        }

        if (PostForm == null && !string.IsNullOrWhiteSpace(Id))
        {
            PostForm ??= new();
            var post = await PostStore.GetPostById(Id);
            if (post != null)
            {
                PostForm!.Title = post.Title;
                PostForm!.ShortTitle = post.ShortTitle;
                PostForm!.Contents = post.MarkdownSource;
                PostForm!.Link = post.Link;
                PostForm!.LinkTitle = post.LinkTitle;
                PostForm!.Tags = string.Join(",", post.Tags.Select(t => t.Name));
            }
        }

        PostForm ??= new();
    }

    private async Task HandleSubmitAsync()
    {
        var post = new Post
        {
            Title = PostForm!.Title,
            ShortTitle = PostForm!.ShortTitle,
            MarkdownSource = PostForm!.Contents,
            Link = PostForm!.Link,
            LinkTitle = PostForm!.LinkTitle,
        };

        List<string> tagNames = PostForm?.Tags?.Split(',')
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList() ?? new();

        if (!string.IsNullOrWhiteSpace(Id))
        {
            try
            {
                var existingPost = await PostStore.GetPostById(Id);
                if (existingPost != null)
                {
                    post.Id = existingPost.Id;
                    post.CreatedDate = existingPost.CreatedDate;
                }

                if (await PostStore.UpdatePostAsync(Id, post, tagNames))
                {
                    OnSuccess("Post updated successfully");
                }
                else
                {
                    OnError("Failed to update post");
                }
            }
            catch (Exception ex)
            {
                OnError($"Failed to update post: {ex.Message}", ex);
            }

            return;
        }

        post.Id = Guid.NewGuid().ToString();
        post.CreatedDate = DateTimeOffset.UtcNow;
        post.LastUpdatedDate = post.CreatedDate;

        try
        {
            if (await PostStore.CreatePostAsync(post, tagNames))
            {
                OnSuccess("Post created successfully");
            }
            else
            {
                OnError("Failed to create post");
            }
        }
        catch (Exception ex)
        {
            OnError($"Failed to create post: {ex.Message}", ex);
        }
    }

    private void ClearForm()
    {
        PostForm = new();
        ErrorMessage = null;
        StateHasChanged();
    }

    private void OnSuccess(string message)
    {
        SuccessMessage = message;
        ErrorMessage = null;
        Logger.LogInformation(SuccessMessage);
        ClearForm();
    }

    private void OnError(string message, Exception? exception = null)
    {
        ErrorMessage = message;
        SuccessMessage = null;

        if (exception != null)
        {
            Logger.LogError(exception, message);
        }
        else
        {
            Logger.LogError(message);
        }
    }
}
```

- [ ] **Step 4: Update `AdminHome.razor.css`**

Open `src/LinkBlog.Web/Components/Pages/Admin/AdminHome.razor.css`. Remove any CSS rules targeting `.trix-*` classes or the draft manager. Keep any existing layout/spacing rules that still apply to the form. If the file only contained Trix/draft styles, it can be cleared to an empty file or retain only the form layout rules.

- [ ] **Step 5: Build to verify no compilation errors**

```bash
dotnet build
```

Expected: Build succeeded.

- [ ] **Step 6: Run all tests**

```bash
dotnet test
```

Expected: All tests pass.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat: replace Trix editor with EasyMDE markdown editor in admin page"
```

---

## Task 11: Build the Migration Tool

This is a standalone one-time tool. It reads all posts from the existing Postgres database and writes them as markdown files to a local output directory for review and upload to Blob Storage.

**Files:**
- Create: `tools/LinkBlog.Migration/LinkBlog.Migration.csproj`
- Create: `tools/LinkBlog.Migration/Program.cs`

The tool should be added to the solution so it builds: `dotnet sln LinkBlog.slnx add tools/LinkBlog.Migration/LinkBlog.Migration.csproj`

- [ ] **Step 1: Create `tools/LinkBlog.Migration/` directory**

```bash
mkdir -p tools/LinkBlog.Migration
```

- [ ] **Step 2: Create `LinkBlog.Migration.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <Description>One-time tool to migrate posts from Postgres to markdown files.</Description>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\LinkBlog.Abstractions\LinkBlog.Abstractions.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.5" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.5" />
    <PackageReference Include="ReverseMarkdown" Version="4.6.0" />
    <PackageReference Include="YamlDotNet" Version="16.3.0" />
  </ItemGroup>

</Project>
```

Note: This tool uses its own package versions since we removed EF Core from the central `Directory.Packages.props`. Either add `<ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>` to its `<PropertyGroup>`, or add the EF Core and ReverseMarkdown package versions back into `Directory.Packages.props` with the tool project in mind. The simplest approach is to disable central package management for this tool project.

Add to the `<PropertyGroup>`:
```xml
<ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
```

- [ ] **Step 3: Create `Program.cs`**

```csharp
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LinkBlog.Abstractions;
using Microsoft.EntityFrameworkCore;
using ReverseMarkdown;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

// Usage: dotnet run -- "<connection-string>" [output-dir]
// Or set DATABASE_URL env var (Heroku postgres:// format supported).
var connectionString = args.Length > 0
    ? args[0]
    : Environment.GetEnvironmentVariable("DATABASE_URL")
      ?? throw new InvalidOperationException("Provide connection string as first argument or DATABASE_URL env var");

// Parse Heroku postgres:// URL format
if (connectionString.StartsWith("postgres://"))
{
    var match = Regex.Match(connectionString, @"postgres://(.*):(.*)@(.*):(.*)/(.*)");
    if (!match.Success) throw new InvalidOperationException("Could not parse DATABASE_URL");
    connectionString = $"Server={match.Groups[3]};Port={match.Groups[4]};User Id={match.Groups[1]};Password={match.Groups[2]};Database={match.Groups[5]};sslmode=Prefer;Trust Server Certificate=true";
}

var outputDir = args.Length > 1 ? args[1] : "output";
Directory.CreateDirectory(outputDir);

var options = new DbContextOptionsBuilder<MigrationDbContext>()
    .UseNpgsql(connectionString)
    .Build();

using var dbContext = new MigrationDbContext(options);

var converter = new Converter(new Config
{
    UnknownTags = Config.UnknownTagsOption.Bypass,
    GithubFlavored = true,
    RemoveComments = true
});

var serializer = new SerializerBuilder()
    .WithNamingConvention(HyphenatedNamingConvention.Instance)
    .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
    .Build();

var pacificZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

var posts = await dbContext.Posts.Include(p => p.Tags).ToListAsync();
Console.WriteLine($"Found {posts.Count} posts to migrate");

int warnings = 0;
foreach (var post in posts)
{
    string markdownBody;

    if (post.Contents.Contains("ridewithgps.com", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"[WARNING] '{post.ShortTitle}' contains RideWithGPS iframe — converting to bare URL for manual review");
        var withBareUrl = Regex.Replace(
            post.Contents,
            @"<iframe[^>]*src=""(https://ridewithgps\.com[^""]+)""[^>]*>.*?</iframe>",
            "\n$1\n",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        markdownBody = converter.Convert(withBareUrl);
        warnings++;
    }
    else if (Regex.IsMatch(post.Contents, @"<(?!(/?(em|strong|a|code|pre|ul|ol|li|p|h[1-6]|blockquote|br)\b))"))
    {
        Console.WriteLine($"[WARNING] '{post.ShortTitle}' contains unusual HTML — review the output");
        markdownBody = converter.Convert(post.Contents);
        warnings++;
    }
    else
    {
        markdownBody = converter.Convert(post.Contents);
    }

    var localDate = TimeZoneInfo.ConvertTime(post.Date, pacificZone);

    var frontmatter = new PostFrontmatter
    {
        Id = post.Id,
        Title = post.Title,
        ShortTitle = post.ShortTitle,
        Type = "post",
        Created = post.Date,
        Updated = post.UpdatedDate,
        Link = post.Link,
        LinkTitle = post.LinkTitle,
        Tags = post.Tags.Select(t => t.Name).ToList(),
        Archived = post.IsArchived ? true : null
    };

    var yaml = serializer.Serialize(frontmatter);
    var fileContent = $"---\n{yaml}---\n\n{markdownBody.Trim()}\n";
    var fileName = $"{localDate:yyyy-MM-dd}-{post.ShortTitle}.md";
    var filePath = Path.Combine(outputDir, fileName);

    await File.WriteAllTextAsync(filePath, fileContent);
    Console.WriteLine($"  Wrote {fileName}");
}

Console.WriteLine($"\nDone. {posts.Count} posts written to '{outputDir}/', {warnings} warning(s).");

// ── EF Core context and entities (local to migration tool) ──────────────

public class MigrationDbContext(DbContextOptions<MigrationDbContext> options) : DbContext(options)
{
    public DbSet<PostEntity> Posts => Set<PostEntity>();
    public DbSet<TagEntity> Tags => Set<TagEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PostEntity>()
            .HasMany(p => p.Tags)
            .WithMany(t => t.Posts)
            .UsingEntity(j => j.ToTable("PostTags"));
    }
}

[Table("Posts")]
public sealed class PostEntity
{
    [Required] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string Title { get; set; } = string.Empty;
    [Required] public string ShortTitle { get; set; } = string.Empty;
    [Required] public DateTimeOffset Date { get; set; }
    [Required] public DateTimeOffset UpdatedDate { get; set; }
    public string? Link { get; set; }
    public string? LinkTitle { get; set; }
    [Required] public string Contents { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
    public List<TagEntity> Tags { get; set; } = new();
}

[Table("Tags")]
public sealed class TagEntity
{
    [Required] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string Name { get; set; } = string.Empty;
    public List<PostEntity> Posts { get; set; } = new();
}

public sealed class PostFrontmatter
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ShortTitle { get; set; } = string.Empty;
    public string Type { get; set; } = "post";
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset Updated { get; set; }
    public string? Link { get; set; }
    public string? LinkTitle { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool? Archived { get; set; }
}
```

- [ ] **Step 4: Add to solution**

```bash
dotnet sln LinkBlog.slnx add tools/LinkBlog.Migration/LinkBlog.Migration.csproj
```

- [ ] **Step 5: Restore and build the migration tool**

```bash
dotnet restore tools/LinkBlog.Migration/LinkBlog.Migration.csproj
dotnet build tools/LinkBlog.Migration/LinkBlog.Migration.csproj
```

Expected: Build succeeded.

- [ ] **Step 6: Run all tests to confirm nothing broken**

```bash
dotnet test
```

Expected: All tests pass.

- [ ] **Step 7: Commit**

```bash
git add tools/LinkBlog.Migration/ LinkBlog.slnx
git commit -m "feat: add one-time migration tool (Postgres → markdown files)"
```

---

## Task 12: Add Nightly Git Backup GitHub Action

This implements the scheduled backup described in the spec: download all `.md` files from the `posts` blob container and commit any changes to the repository.

**Files:**
- Create: `.github/workflows/backup-posts.yml`

- [ ] **Step 1: Create the workflow file**

```bash
mkdir -p .github/workflows
```

Create `.github/workflows/backup-posts.yml`:

```yaml
name: Backup posts from Blob Storage to git

on:
  schedule:
    - cron: '0 6 * * *'  # Daily at 6 AM UTC
  workflow_dispatch:       # Allow manual trigger

jobs:
  backup:
    runs-on: ubuntu-latest
    permissions:
      contents: write

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Download posts from Azure Blob Storage
        env:
          AZURE_STORAGE_ACCOUNT: ${{ secrets.AZURE_STORAGE_ACCOUNT }}
          AZURE_STORAGE_KEY: ${{ secrets.AZURE_STORAGE_KEY }}
        run: |
          mkdir -p posts
          az storage blob download-batch \
            --account-name "$AZURE_STORAGE_ACCOUNT" \
            --account-key "$AZURE_STORAGE_KEY" \
            --source posts \
            --destination posts/ \
            --overwrite

      - name: Commit changes if any
        run: |
          git config user.name "github-actions[bot]"
          git config user.email "github-actions[bot]@users.noreply.github.com"
          git add posts/
          if git diff --cached --quiet; then
            echo "No changes to commit"
          else
            git commit -m "chore: backup posts from Blob Storage"
            git push
          fi
```

**Prerequisites:** Add `AZURE_STORAGE_ACCOUNT` and `AZURE_STORAGE_KEY` as repository secrets in GitHub Settings → Secrets and variables → Actions.

- [ ] **Step 2: Build and test**

```bash
dotnet build
```

Expected: Build succeeded (no C# changes — this step just adds the workflow file).

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/backup-posts.yml
git commit -m "chore: add nightly GitHub Action to back up posts from Blob Storage to git"
```

> **Note on `PostCacheRefreshService`:** The spec calls for ETag-based change detection to avoid re-downloading unchanged blobs every N minutes. The current plan keeps the existing timer-based full refresh (every 5 minutes by default). For a personal blog this is acceptable — a future optimization can add ETag checking if blob download volume becomes a concern.

---

## Post-Implementation: Data Migration Checklist

After all tasks are complete, run the migration before going live:

- [ ] Run `dotnet run --project tools/LinkBlog.Migration -- "<connection-string>" ./migrated-posts`
- [ ] Review the `migrated-posts/` output directory — spot-check several posts for correct rendering
- [ ] Pay special attention to any `[WARNING]` posts (RideWithGPS, unusual HTML)
- [ ] Commit the `migrated-posts/` directory to the git repository (or a `posts/` subfolder)
- [ ] Upload all `.md` files to the Azure Blob `posts` container
- [ ] Start the app locally with `aspire run` and verify posts load and render correctly
- [ ] Deploy to Heroku
- [ ] Verify production posts render correctly
- [ ] Decommission the Postgres add-on on Heroku
