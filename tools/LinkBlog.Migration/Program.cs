using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;
using LinkBlog.Migration;
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
if (connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
{
    var match = Regex.Match(connectionString, @"^postgres://([^:]+):([^@]+)@([^:]+):(\d+)/([^?]+)");
    if (!match.Success) throw new InvalidOperationException("Could not parse DATABASE_URL");
    connectionString = $"Server={match.Groups[3]};Port={match.Groups[4]};User Id={match.Groups[1]};Password={match.Groups[2]};Database={match.Groups[5]};sslmode=Prefer;Trust Server Certificate=true";
}

var outputDir = args.Length > 1 ? args[1] : "output";
Directory.CreateDirectory(outputDir);

var contextOptions = new DbContextOptionsBuilder<MigrationDbContext>()
    .UseNpgsql(connectionString)
    .Options;

using var dbContext = new MigrationDbContext(contextOptions);

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

var pacificZone = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");

var posts = await dbContext.Posts.Include(p => p.Tags).ToListAsync();
Console.WriteLine($"Found {posts.Count} posts to migrate");

int warnings = 0;
foreach (var post in posts)
{
    string markdownBody;

    if (string.IsNullOrEmpty(post.Contents))
    {
        Console.WriteLine($"[WARNING] '{post.ShortTitle}' has empty Contents — skipping");
        warnings++;
        continue;
    }

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
        Created = post.Date.ToString("o"),
        Updated = post.UpdatedDate.ToString("o"),
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

namespace LinkBlog.Migration
{
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
        public string Created { get; set; } = string.Empty;
        public string Updated { get; set; } = string.Empty;
        public string? Link { get; set; }
        public string? LinkTitle { get; set; }
        public List<string> Tags { get; set; } = new();
        public bool? Archived { get; set; }
    }
}