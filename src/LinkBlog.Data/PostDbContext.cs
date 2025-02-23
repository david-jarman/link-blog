using Microsoft.EntityFrameworkCore;

namespace LinkBlog.Data;

public class PostDbContext : DbContext
{
    public PostDbContext(DbContextOptions<PostDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Renamed the classes to PostEntity and TagEntity
        // Can't rely on the EF conventions for the join table
        // and must explicitely name it.
        modelBuilder.Entity<PostEntity>()
            .HasMany(e => e.Tags)
            .WithMany(e => e.Posts)
            .UsingEntity("PostTag");
    }

    public DbSet<PostEntity> Posts { get; set; } = null!;

    public DbSet<TagEntity> Tags { get; set; } = null!;
}