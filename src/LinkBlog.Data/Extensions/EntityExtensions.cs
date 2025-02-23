using LinkBlog.Abstractions;

namespace LinkBlog.Data;

internal static class EntityExtensions
{
    public static Post ToPost(this PostEntity postEntity)
    {
        return new Post()
        {
            Id = postEntity.Id,
            Title = postEntity.Title,
            ShortTitle = postEntity.ShortTitle,
            CreatedDate = postEntity.Date,
            Link = postEntity.Link,
            LinkTitle = postEntity.LinkTitle,
            Contents = postEntity.Contents,
            Tags = postEntity.Tags.Select(t => t.ToTag())
        };
    }

    public static Tag ToTag(this TagEntity tagEntity)
    {
        return new Tag()
        {
            Id = tagEntity.Id,
            Name = tagEntity.Name,
            Posts = tagEntity.Posts.Select(p => p.ToPost())
        };
    }

    public static PostEntity ToPostEntity(this Post post)
    {
        return new PostEntity()
        {
            Id = post.Id,
            Title = post.Title,
            ShortTitle = post.ShortTitle,
            Date = post.CreatedDate,
            Link = post.Link,
            LinkTitle = post.LinkTitle,
            Contents = post.Contents,
            Tags = post.Tags.Select(t => t.ToTagEntity()).ToList()
        };
    }

    public static TagEntity ToTagEntity(this Tag tag)
    {
        return new TagEntity()
        {
            Id = tag.Id,
            Name = tag.Name,
            Posts = tag.Posts.Select(p => p.ToPostEntity()).ToList()
        };
    }
}