using System.Runtime.CompilerServices;
using LinkBlog.Data;
using Microsoft.EntityFrameworkCore;

namespace LinkBlog.Web.Services;

public interface IPostStore
{
    IAsyncEnumerable<Post> GetPosts(int topN, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Post> GetPostsForTag(string tag, CancellationToken cancellationToken = default);

    Task<bool> CreatePostAsync(Post post, CancellationToken cancellationToken = default);

    Task<Tag?> GetTagAsync(string tag, CancellationToken cancellationToken = default);

    Task<bool> CreateTagAsync(Tag tag, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Post> GetPostsForDateRange(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default);

    Task<Post?> GetPostForDateRangeAndShortTitleAsync(DateTimeOffset start, DateTimeOffset end, string shortTitle, CancellationToken cancellationToken = default);
}

public class PostStoreDb : IPostStore
{
    private readonly PostDbContext postDbContext;

    public PostStoreDb(PostDbContext postDbContext)
    {
        this.postDbContext = postDbContext;
    }

    public async Task<bool> CreatePostAsync(Post post, CancellationToken cancellationToken = default)
    {
        this.postDbContext.Posts.Add(post);
        await this.postDbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CreateTagAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        this.postDbContext.Tags.Add(tag);

        await this.postDbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async IAsyncEnumerable<Post> GetPosts(int topN, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var posts = this.postDbContext.Posts
            .Include(p => p.Tags)
            .OrderByDescending(p => p.Date)
            .Take(topN)
            .AsAsyncEnumerable();

         await foreach (var post in posts)
         {
            cancellationToken.ThrowIfCancellationRequested();

            yield return post;
         }
    }

    public async IAsyncEnumerable<Post> GetPostsForTag(string tag, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Tag? tagFromDb = await this.postDbContext.Tags
            .Include(t => t.Posts)
            .ThenInclude(p => p.Tags)
            .FirstAsync(t => t.Name == tag, cancellationToken);
        if (tagFromDb == null)
        {
            yield break;
        }

        foreach (var post in tagFromDb.Posts.OrderByDescending(p => p.Date))
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return post;
        }
    }

    public async IAsyncEnumerable<Post> GetPostsForDateRange(DateTimeOffset start, DateTimeOffset end, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var posts = this.postDbContext.Posts
            .Include(p => p.Tags)
            .Where(p => p.Date >= start && p.Date <= end)
            .OrderByDescending(p => p.Date)
            .AsAsyncEnumerable();

        await foreach (var post in posts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return post;
        }
    }

    public Task<Tag?> GetTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        return this.postDbContext.Tags
            .FirstOrDefaultAsync(t => t.Name == tag, cancellationToken);
    }

    public async Task<Post?> GetPostForDateRangeAndShortTitleAsync(DateTimeOffset start, DateTimeOffset end, string shortTitle, CancellationToken cancellationToken = default)
    {
        return await this.postDbContext.Posts
            .Include(p => p.Tags)
            .SingleOrDefaultAsync(p => p.Date >= start && p.Date <= end && p.ShortTitle == shortTitle);
    }
}
