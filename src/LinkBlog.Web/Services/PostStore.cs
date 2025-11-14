using System.Runtime.CompilerServices;
using LinkBlog.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace LinkBlog.Data;

public interface IPostStore
{
    IAsyncEnumerable<Post> GetPosts(int topN, CancellationToken cancellationToken = default);

    Task<Post?> GetPostById(string id, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Post> GetPostsForTag(string tag, CancellationToken cancellationToken = default);

    Task<bool> CreatePostAsync(Post post, List<string> tags, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Post> GetPostsForDateRange(DateTimeOffset startDateTime, DateTimeOffset endDateTime, CancellationToken cancellationToken = default);

    Task<Post?> GetPostForShortTitleAsync(string shortTitle, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Post> SearchPostsAsync(string searchQuery, int maxResults = 50, CancellationToken cancellationToken = default);

    Task<bool> UpdatePostAsync(string id, Post post, List<string> tags, CancellationToken cancellationToken = default);

    Task<bool> ArchivePostAsync(string id, CancellationToken cancellationToken = default);
}

public class PostStoreDb : IPostStore
{
    private readonly PostDbContext postDbContext;

    public PostStoreDb(PostDbContext postDbContext)
    {
        this.postDbContext = postDbContext;
    }

    public async Task<bool> CreatePostAsync(Post post, List<string> tags, CancellationToken cancellationToken = default)
    {
        // Find or create tags first
        PostEntity entity = post.ToPostEntity();
        var tagEntities = await GetOrCreateTagsAsync(tags, cancellationToken);
        entity.Tags = tagEntities;

        this.postDbContext.Posts.Add(entity);
        await this.postDbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async IAsyncEnumerable<Post> GetPosts(int topN, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var posts = this.postDbContext.Posts
            .Include(p => p.Tags)
            .Where(p => !p.IsArchived)
            .OrderByDescending(p => p.Date)
            .Take(topN)
            .AsAsyncEnumerable();

        await foreach (var post in posts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return post.ToPost();
        }
    }

    public async IAsyncEnumerable<Post> GetPostsForTag(string tag, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        TagEntity? tagFromDb = await this.postDbContext.Tags
            .Include(t => t.Posts)
            .ThenInclude(p => p.Tags)
            .FirstAsync(t => t.Name == tag, cancellationToken);
        if (tagFromDb == null)
        {
            yield break;
        }

        foreach (var post in tagFromDb.Posts.Where(p => !p.IsArchived).OrderByDescending(p => p.Date))
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return post.ToPost();
        }
    }

    public async IAsyncEnumerable<Post> GetPostsForDateRange(DateTimeOffset startDateTime, DateTimeOffset endDateTime, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var posts = this.postDbContext.Posts
            .Include(p => p.Tags)
            .Where(p => p.Date >= startDateTime && p.Date <= endDateTime && !p.IsArchived)
            .OrderByDescending(p => p.Date)
            .AsAsyncEnumerable();

        await foreach (var post in posts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return post.ToPost();
        }
    }

    public async Task<Post?> GetPostForShortTitleAsync(string shortTitle, CancellationToken cancellationToken = default)
    {
        var post = await this.postDbContext.Posts
            .Include(p => p.Tags)
            .SingleOrDefaultAsync(p => p.ShortTitle == shortTitle && !p.IsArchived, cancellationToken: cancellationToken);

        return post?.ToPost();
    }

    public async IAsyncEnumerable<Post> SearchPostsAsync(string searchQuery, int maxResults = 50, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            yield break;
        }

        // Use PostgreSQL full-text search with the precomputed SearchVector column
        // SearchVector is a computed column with weighted tsvector (Title: A, LinkTitle: B, Contents: C)
        var posts = this.postDbContext.Posts
            .FromSqlRaw(@"
                SELECT p.*
                FROM ""Posts"" p
                WHERE p.""IsArchived"" = false
                AND p.""SearchVector"" @@ plainto_tsquery('english', {0})
                ORDER BY ts_rank(p.""SearchVector"", plainto_tsquery('english', {0})) DESC
                LIMIT {1}",
                searchQuery,
                maxResults)
            .Include(p => p.Tags)
            .AsAsyncEnumerable();

        await foreach (var post in posts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return post.ToPost();
        }
    }

    public Task<Post?> GetPostById(string id, CancellationToken cancellationToken = default)
    {
        return this.postDbContext.Posts
            .Include(p => p.Tags)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ContinueWith(t => t.Result?.ToPost());
    }

    public async Task<bool> UpdatePostAsync(string id, Post post, List<string> tags, CancellationToken cancellationToken = default)
    {
        var postEntity = await this.postDbContext.Posts
            .Include(p => p.Tags)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (postEntity == null)
        {
            return false;
        }

        // Update the post entity with the new values
        postEntity.Title = post.Title;
        postEntity.Link = post.Link;
        postEntity.LinkTitle = post.LinkTitle;
        postEntity.Contents = post.Contents;
        postEntity.UpdatedDate = DateTimeOffset.UtcNow;

        // Compare tags and add or remove as necessary
        var currentTags = postEntity.Tags.Select(t => t.Name).ToList();
        var newTags = tags.Except(currentTags).ToList();
        var removedTags = currentTags.Except(tags).ToList();

        // Remove tags
        foreach (var removedTag in removedTags)
        {
            var tagEntityToRemove = postEntity.Tags.First(t => t.Name == removedTag);
            postEntity.Tags.Remove(tagEntityToRemove);
        }

        // Add new tags
        foreach (var newTag in newTags)
        {
            // Check for existing tag first
            var existingTag = await this.postDbContext.Tags
                .FirstOrDefaultAsync(t => t.Name == newTag, cancellationToken);
            if (existingTag != null)
            {
                postEntity.Tags.Add(existingTag);
            }
            else
            {
                // Create tag and add
                var tagEntityToAdd = new TagEntity()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = newTag
                };
                postEntity.Tags.Add(tagEntityToAdd);
            }
        }

        // Save changes
        await this.postDbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> ArchivePostAsync(string id, CancellationToken cancellationToken = default)
    {
        var postEntity = await this.postDbContext.Posts
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (postEntity == null)
        {
            return false;
        }

        postEntity.IsArchived = true;
        postEntity.UpdatedDate = DateTimeOffset.UtcNow;

        await this.postDbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<List<TagEntity>> GetOrCreateTagsAsync(List<string> tags, CancellationToken cancellationToken)
    {
        List<TagEntity> tagEntities = new();
        foreach (var tag in tags)
        {
            TagEntity? tagEntity = await this.postDbContext.Tags
                .FirstOrDefaultAsync(t => t.Name == tag, cancellationToken);
            if (tagEntity == null)
            {
                tagEntity = new TagEntity()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = tag
                };
                this.postDbContext.Tags.Add(tagEntity);
            }

            tagEntities.Add(tagEntity);
        }

        return tagEntities;
    }
}