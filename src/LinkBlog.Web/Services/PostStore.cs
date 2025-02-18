using System.Runtime.CompilerServices;
using LinkBlog.Contracts;
using Microsoft.EntityFrameworkCore;

namespace LinkBlog.Web.Services;

public interface IPostStore
{
    IAsyncEnumerable<Post> GetPosts(CancellationToken cancellationToken = default);

    IAsyncEnumerable<Post> GetPostsForTag(string tag, CancellationToken cancellationToken = default);

    Task<bool> CreatePost(Post post, CancellationToken cancellationToken = default);
}

public class PostStoreDb : IPostStore
{
    private readonly PostDbContext postDbContext;

    public PostStoreDb(PostDbContext postDbContext)
    {
        this.postDbContext = postDbContext;
    }

    public async Task<bool> CreatePost(Post post, CancellationToken cancellationToken = default)
    {
        this.postDbContext.Posts.Add(post);
        int entries = await this.postDbContext.SaveChangesAsync(cancellationToken);
        return entries == 1;
    }

    public async IAsyncEnumerable<Post> GetPosts([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var posts = this.postDbContext.Posts
            .Include(p => p.Tags)
            .OrderByDescending(p => p.Date)
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
}
