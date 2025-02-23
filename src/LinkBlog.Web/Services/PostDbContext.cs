using LinkBlog.Data;
using Microsoft.EntityFrameworkCore;

namespace LinkBlog.Web.Services;

public class PostDbContext : DbContext
{
    public PostDbContext(DbContextOptions<PostDbContext> options)
        : base(options)
    {
    }

    public DbSet<PostEntity> Posts { get; set; } = null!;

    public DbSet<Tag> Tags { get; set; } = null!;
}