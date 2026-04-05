using LinkBlog.Abstractions;
using Markdig;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LinkBlog.Data;

public sealed class PostMarkdownSerializer
{
    private static readonly TimeZoneInfo PacificZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");

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
        if (string.IsNullOrWhiteSpace(frontmatterYaml))
        {
            throw new InvalidOperationException("Markdown file is missing YAML frontmatter. Expected content starting with '---'.");
        }
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
            Tags = (fm.Tags ?? []).Select(t => new Tag { Id = t, Name = t }),
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
            Updated = post.LastUpdatedDate,
            Link = post.Link,
            LinkTitle = post.LinkTitle,
            Tags = post.Tags.Select(t => t.Name).ToList(),
            Archived = post.IsArchived,
        };

        var yaml = yamlSerializer.Serialize(fm);
        return $"---\n{yaml}---\n\n{post.MarkdownSource}";
    }

    public static string GetBlobName(Post post)
    {
        var local = TimeZoneInfo.ConvertTime(post.CreatedDate, PacificZone);
        return $"{local:yyyy-MM-dd}-{post.ShortTitle}.md";
    }

    private static (string frontmatter, string body) SplitFrontmatter(string content)
    {
        // Normalize CRLF to LF
        content = content.Replace("\r\n", "\n", StringComparison.Ordinal);

        if (!content.StartsWith("---\n", StringComparison.Ordinal))
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