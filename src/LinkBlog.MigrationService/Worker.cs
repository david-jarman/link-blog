using System.Diagnostics;
using LinkBlog.Data;
using Microsoft.EntityFrameworkCore;

namespace LinkBlog.MigrationService;

public class Worker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime) : BackgroundService
{
    public static readonly ActivitySource ActivitySource = new("LinkBlog.MigrationService");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = ActivitySource.StartActivity("Migrating database", ActivityKind.Client);

        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PostDbContext>();

            await dbContext.Database.MigrateAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }

        // Stop the application once migration is complete
        hostApplicationLifetime.StopApplication();
    }
}