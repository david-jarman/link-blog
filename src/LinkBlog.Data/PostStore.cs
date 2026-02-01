using System.Runtime.CompilerServices;
using LinkBlog.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace LinkBlog.Data;

public record PagedPostsResult(Post[] Posts, bool HasMore);

public interface IPostStore
{
    IAsyncEnumerable<Post> GetPosts(int topN, CancellationToken cancellationToken = default);

    Task<PagedPostsResult> GetPostsPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<Post?> GetPostById(string id, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Post> GetPostsForTag(string tag, CancellationToken cancellationToken = default);

    Task<bool> CreatePostAsync(Post post, List<string> tags, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Post> GetPostsForDateRange(DateTimeOffset startDateTime, DateTimeOffset endDateTime, CancellationToken cancellationToken = default);

    Task<Post?> GetPostForShortTitleAsync(string shortTitle, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Post> SearchPostsAsync(string searchQuery, int maxResults = 50, CancellationToken cancellationToken = default);

    Task<bool> UpdatePostAsync(string id, Post post, List<string> tags, CancellationToken cancellationToken = default);

    Task<bool> ArchivePostAsync(string id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Database access layer for posts. Used by CachedPostStore for loading posts and persisting writes.
/// </summary>
public class PostDataAccess : IPostDataAccess
{
    private readonly PostDbContext postDbContext;

    public PostDataAccess(PostDbContext postDbContext)
    {
        this.postDbContext = postDbContext;
    }

    public async IAsyncEnumerable<Post> GetAllPostsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var posts = this.postDbContext.Posts
            .Include(p => p.Tags)
            .Where(p => !p.IsArchived)
            .OrderByDescending(p => p.Date)
            .AsAsyncEnumerable();

        await foreach (var post in posts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return post.ToPost();
        }
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
