using System.Globalization;
using System.Text;
using LinkBlog.Abstractions;
using Microsoft.Extensions.Options;

namespace LinkBlog.Feed;

public interface ISyndicationFeed
{
    string GetXmlForPosts(IEnumerable<Post> posts);
}

public class AtomFeed : ISyndicationFeed
{
    private readonly FeedOptions _options;

    public AtomFeed(IOptions<FeedOptions> options)
    {
        _options = options.Value;
    }

    public string GetXmlForPosts(IEnumerable<Post> posts)
    {
        StringBuilder sb = new();

        sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.Append("<feed xmlns=\"http://www.w3.org/2005/Atom\">");
        sb.Append(string.Create(CultureInfo.InvariantCulture, $"<title>{_options.BlogTitle}</title>"));
        sb.Append(string.Create(CultureInfo.InvariantCulture, $"<link href=\"{_options.BlogUrl}/\" rel=\"alternate\"/>"));
        sb.Append(string.Create(CultureInfo.InvariantCulture, $"<id>{_options.BlogUrl}/</id>"));
        sb.Append(string.Create(CultureInfo.InvariantCulture, $"<author><name>{_options.AuthorName}</name></author>"));

        var latestPost = posts.First();
        sb.Append(CultureInfo.InvariantCulture, $"<updated>{latestPost.CreatedDate.ToString("o")}</updated>");

        foreach (var post in posts)
        {
            sb.Append(CreateEntryForPost(post));
        }

        sb.Append("</feed>");

        return sb.ToString();
    }

    private StringBuilder CreateEntryForPost(Post post)
    {
        StringBuilder sb = new();
        string postUrl = $"{_options.BlogUrl}{post.UrlPath}";

        sb.Append("<entry>");

        // Required elements
        sb.Append(CultureInfo.InvariantCulture, $"<id>{postUrl}</id>");
        sb.Append(CultureInfo.InvariantCulture, $"<title>{post.Title}</title>");
        sb.Append(CultureInfo.InvariantCulture, $"<updated>{post.LastUpdatedDate.ToString("o")}</updated>");

        // Recommended elements
        sb.Append(CultureInfo.InvariantCulture, $"<content type=\"html\">{System.Net.WebUtility.HtmlEncode(post.Contents)}</content>");
        sb.Append(CultureInfo.InvariantCulture, $"<link href=\"{postUrl}\" rel=\"alternate\"/>");

        // Optional elements
        sb.Append(CultureInfo.InvariantCulture, $"<published>{post.CreatedDate.ToString("o")}</published>");
        foreach (Tag tag in post.Tags)
        {
            sb.Append(CultureInfo.InvariantCulture, $"<category term=\"{tag.Name}\" />");
        }

        sb.Append("</entry>");

        return sb;
    }
}