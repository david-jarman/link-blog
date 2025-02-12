using LinkBlog.Contracts;

namespace LinkBlog.Web;

public class PostsClient(HttpClient httpClient)
{
    public async Task<Post[]> GetPostsAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        List<Post>? posts = null;

        await foreach (var post in httpClient.GetFromJsonAsAsyncEnumerable<Post>("/api/posts", cancellationToken))
        {
            if (posts?.Count >= maxItems)
            {
                break;
            }
            if (post is not null)
            {
                posts ??= [];
                posts.Add(post);
            }
        }

        return posts?.ToArray() ?? [];
    }

    public IAsyncEnumerable<Post?> GetPostsForTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        return httpClient.GetFromJsonAsAsyncEnumerable<Post>($"/api/posts/tag/{tag}", cancellationToken);
    }
}
