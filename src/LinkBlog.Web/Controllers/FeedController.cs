using LinkBlog.Abstractions;
using LinkBlog.Data;
using LinkBlog.Feed;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LinkBlog.Web.Controllers;

public class FeedController : Controller
{
    private readonly IPostStore postStore;
    private readonly ISyndicationFeed feed;
    private readonly IOptions<FeedOptions> options;

    public FeedController(IPostStore postStore, ISyndicationFeed feed, IOptions<FeedOptions> options)
    {
        this.postStore = postStore;
        this.feed = feed;
        this.options = options;
    }

    [HttpGet("/atom/all")]
    public async Task<IActionResult> GetAllAsync(CancellationToken ct)
    {
        List<Post> posts = new();
        var postsFromDb = postStore.GetPosts(options.Value.MaxPostCount, ct);
        await foreach (var post in postsFromDb)
        {
            posts.Add(post);
        }

        Response.Headers["Content-Type"] = "application/xml; charset=utf-8";

        // Prevent browsers from trying to automatically open the feed in an RSS reader
        // Useful for debugging the feed locally.
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        return Content(feed.GetXmlForPosts(posts));
    }
}