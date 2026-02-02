using System.Diagnostics;
using System.Globalization;
using System.Text;
using Bogus;
using LinkBlog.Data;
using Microsoft.EntityFrameworkCore;

namespace LinkBlog.MigrationService;

public partial class DatabaseSeeder(PostDbContext dbContext, ILogger<DatabaseSeeder> logger)
{
    public static readonly ActivitySource ActivitySource = new("LinkBlog.MigrationService.Seeder");

    private const int RandomSeed = 42;
    private const int PostCount = 50;

    private static readonly string[] TagNames =
    [
        "csharp", "dotnet", "blazor", "aspnetcore", "entityframework",
        "postgresql", "docker", "kubernetes", "azure", "aws",
        "typescript", "javascript", "react", "vue", "angular",
        "testing", "devops", "architecture"
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("Seeding database", ActivityKind.Client);

        if (await dbContext.Posts.AnyAsync(cancellationToken))
        {
            LogSkippingSeed(logger);
            return;
        }

        LogStartingSeed(logger);

        var tags = await CreateTagsAsync(cancellationToken);
        await CreatePostsAsync(tags, cancellationToken);

        LogSeedComplete(logger);
    }

    private async Task<List<TagEntity>> CreateTagsAsync(CancellationToken cancellationToken)
    {
        var tags = TagNames.Select(name => new TagEntity
        {
            Id = Guid.NewGuid().ToString(),
            Name = name
        }).ToList();

        dbContext.Tags.AddRange(tags);
        await dbContext.SaveChangesAsync(cancellationToken);

        LogCreatedTags(logger, tags.Count);
        return tags;
    }

    private async Task<List<PostEntity>> CreatePostsAsync(List<TagEntity> tags, CancellationToken cancellationToken)
    {
        Randomizer.Seed = new Random(RandomSeed);

        var faker = new Faker<PostEntity>()
            .RuleFor(p => p.Id, f => Guid.NewGuid().ToString())
            .RuleFor(p => p.Title, f => GenerateTitle(f))
            .RuleFor(p => p.ShortTitle, (f, p) => GenerateShortTitle(p.Title, p.Id))
            .RuleFor(p => p.Date, f => f.Date.PastOffset(1).ToUniversalTime())
            .RuleFor(p => p.UpdatedDate, (f, p) => p.Date)
            .RuleFor(p => p.Link, f => f.Random.Bool(0.7f) ? f.Internet.Url() : null)
            .RuleFor(p => p.LinkTitle, (f, p) => p.Link != null ? f.Company.CatchPhrase() : null)
            .RuleFor(p => p.Contents, f => GenerateMarkdownContent(f))
            .RuleFor(p => p.IsArchived, f => false)
            .RuleFor(p => p.Tags, f => f.PickRandom(tags, f.Random.Int(1, 4)).ToList());

        var posts = faker.Generate(PostCount);

        dbContext.Posts.AddRange(posts);
        await dbContext.SaveChangesAsync(cancellationToken);

        LogCreatedPosts(logger, posts.Count);
        return posts;
    }

    private static string GenerateTitle(Faker f)
    {
        var templates = new[]
        {
            () => $"How to {f.Hacker.Verb()} {f.Hacker.Noun()} in {f.PickRandom(TagNames)}",
            () => $"Understanding {f.Hacker.Adjective()} {f.Hacker.Noun()}",
            () => $"Building a {f.Hacker.Adjective()} {f.Hacker.Noun()} with {f.PickRandom(TagNames)}",
            () => $"Why {f.Hacker.Noun()} matters for {f.Hacker.Noun()}",
            () => $"Getting started with {f.PickRandom(TagNames)}",
            () => $"Deep dive into {f.Hacker.Noun()}",
            () => $"Best practices for {f.Hacker.IngVerb()} {f.Hacker.Noun()}",
            () => $"The complete guide to {f.Hacker.Noun()}"
        };

        return f.PickRandom(templates)();
    }

    private static string GenerateShortTitle(string title, string id)
    {
        var slug = title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace(".", "")
            .Replace(",", "")
            .Replace("?", "")
            .Replace("!", "");

        // Append first 6 chars of ID to ensure uniqueness
        return $"{slug}-{id[..6]}";
    }

    private static string GenerateMarkdownContent(Faker f)
    {
        var sb = new StringBuilder();

        // Opening paragraph
        sb.AppendLine(f.Lorem.Paragraph(3));
        sb.AppendLine();

        // Add a heading and paragraph
        sb.Append(CultureInfo.InvariantCulture, $"## {f.Lorem.Sentence(3).TrimEnd('.')}");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine(f.Lorem.Paragraph(4));
        sb.AppendLine();

        // Sometimes add a code block
        if (f.Random.Bool(0.6f))
        {
            sb.AppendLine("```csharp");
            sb.Append(CultureInfo.InvariantCulture, $"public class {f.Hacker.Noun().Replace(" ", "")}");
            sb.AppendLine();
            sb.AppendLine("{");
            sb.Append(CultureInfo.InvariantCulture, $"    public string {f.Hacker.Noun().Replace(" ", "")} {{ get; set; }}");
            sb.AppendLine();
            sb.AppendLine("}");
            sb.AppendLine("```");
            sb.AppendLine();
        }

        // Add another paragraph
        sb.AppendLine(f.Lorem.Paragraph(3));
        sb.AppendLine();

        // Sometimes add a list
        if (f.Random.Bool(0.5f))
        {
            sb.AppendLine("## Key points");
            sb.AppendLine();
            for (var i = 0; i < f.Random.Int(3, 5); i++)
            {
                sb.Append(CultureInfo.InvariantCulture, $"- {f.Lorem.Sentence()}");
                sb.AppendLine();
            }
            sb.AppendLine();
        }

        // Sometimes add a quote
        if (f.Random.Bool(0.3f))
        {
            sb.Append(CultureInfo.InvariantCulture, $"> {f.Lorem.Sentence()}");
            sb.AppendLine();
            sb.AppendLine();
        }

        // Closing paragraph
        sb.AppendLine(f.Lorem.Paragraph(2));

        return sb.ToString();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Database already contains data, skipping seed")]
    private static partial void LogSkippingSeed(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeding database with fake data")]
    private static partial void LogStartingSeed(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Database seeding complete")]
    private static partial void LogSeedComplete(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created {Count} tags")]
    private static partial void LogCreatedTags(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created {Count} posts")]
    private static partial void LogCreatedPosts(ILogger logger, int count);
}