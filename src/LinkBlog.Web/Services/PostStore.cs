using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LinkBlog.Contracts;
using Microsoft.EntityFrameworkCore;

namespace LinkBlog.Web.Services;

public interface IPostStore
{
    IAsyncEnumerable<Post> GetPosts(CancellationToken cancellationToken = default);

    IAsyncEnumerable<Post> GetPostsForTag(string tag, CancellationToken cancellationToken = default);
}

public class PostStoreDb : IPostStore
{
    private readonly PostDbContext postDbContext;

    public PostStoreDb(PostDbContext postDbContext)
    {
        this.postDbContext = postDbContext;
    }

    public async IAsyncEnumerable<Post> GetPosts([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
         await foreach (var post in this.postDbContext.Posts.AsAsyncEnumerable())
         {
            cancellationToken.ThrowIfCancellationRequested();

            yield return post;
         }
    }

    public async IAsyncEnumerable<Post> GetPostsForTag(string tag, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Tag? tagFromDb = await this.postDbContext.Tags.FirstAsync(t => t.Name == tag, cancellationToken);
        if (tagFromDb == null)
        {
            yield break;
        }

        foreach (var post in tagFromDb.Posts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return post;
        }
    }
}
