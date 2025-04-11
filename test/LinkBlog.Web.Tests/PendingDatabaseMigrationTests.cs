using LinkBlog.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LinkBlog.Web.Tests;

public class PendingDatabaseMigrationTests
{
    [Fact]
    public void Database_HasNoPendingModelChanges()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .AddDbContext<PostDbContext>(options =>
                options.UseNpgsql())
            .BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PostDbContext>();

        // Act
        bool hasPendingModelChanges = context.Database.HasPendingModelChanges();

        // Assert
        Assert.False(hasPendingModelChanges, "Database has pending model changes that require a new migration");
    }
}