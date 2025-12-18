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

        // Configure full-text search computed column
        // Weight: Title (A - highest), LinkTitle (B - medium), Contents (C - lowest)
        modelBuilder.Entity<PostEntity>()
            .Property(p => p.SearchVector)
            .HasComputedColumnSql(
                @"setweight(to_tsvector('english', COALESCE(""Title"", '')), 'A') ||
                  setweight(to_tsvector('english', COALESCE(""LinkTitle"", '')), 'B') ||
                  setweight(to_tsvector('english', COALESCE(""Contents"", '')), 'C')",
                stored: true);

        // Add GIN index for fast full-text search
        modelBuilder.Entity<PostEntity>()
            .HasIndex(p => p.SearchVector)
            .HasMethod("GIN");
    }

    public DbSet<PostEntity> Posts { get; set; } = null!;

    public DbSet<TagEntity> Tags { get; set; } = null!;
}