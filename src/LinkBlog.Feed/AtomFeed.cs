using System.Text;
using LinkBlog.Contracts;

namespace LinkBlog.Feed;

public interface ISyndicationFeed
{
    string GetXmlForPosts(IEnumerable<Post> posts);
}

public class AtomFeed : ISyndicationFeed
{
    private readonly TimeZoneInfo pacificZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
    private readonly string blogUrl = "https://davidjarman.net";

    public string GetXmlForPosts(IEnumerable<Post> posts)
    {
        StringBuilder sb = new();

        // TODO: Remove hardcoded elements and pass them in to class from LinkBlog.Web
        sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.Append("<feed xmlns=\"http://www.w3.org/2005/Atom\">");
        sb.Append("<title>David Jarman's Blog</title>");
        sb.Append($"<link href=\"{blogUrl}/\" rel=\"alternate\"/>");
        sb.Append($"<id>{blogUrl}/</id>");
        sb.Append("<author><name>David Jarman</name></author>");

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
        DateTimeOffset postDate = TimeZoneInfo.ConvertTime(post.Date, pacificZone);
        string postUrl = $"{blogUrl}/{postDate.Year}/{postDate.Month}/{postDate.Day}/{post.ShortTitle}/";

        sb.Append("<entry>");

        // Required elements
        sb.Append($"<id>{postUrl}</id>");
        sb.Append($"<title>{post.Title}</title>");
        sb.Append($"<updated>{post.Date.ToString("o")}</updated>");

        // Recommended elements
        sb.Append($"<content type=\"html\">{System.Net.WebUtility.HtmlEncode(post.Contents)}</content>");
        sb.Append($"<link href=\"{postUrl}\" rel=\"alternate\"/>");

        // Optional elements
        sb.Append($"<published>{post.Date.ToString("o")}</published>");
        foreach (Tag tag in post.Tags)
        {
            sb.Append($"<category term=\"{tag.Name}\" />");
        }

        sb.Append("</entry>");

        return sb;
    }
}
