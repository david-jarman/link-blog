using System.Diagnostics;
using LinkBlog.Data;
using Microsoft.EntityFrameworkCore;

namespace LinkBlog.MigrationService;

public partial class Worker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime,
    IHostEnvironment hostEnvironment,
    ILogger<Worker> logger) : BackgroundService
{
    public static readonly ActivitySource ActivitySource = new("LinkBlog.MigrationService");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = ActivitySource.StartActivity("Migrating database", ActivityKind.Client);

        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PostDbContext>();

            LogMigrationStarting(logger);
            await dbContext.Database.MigrateAsync(stoppingToken);
            LogMigrationComplete(logger);

            // Seed database in development environment
            LogEnvironmentCheck(logger, hostEnvironment.EnvironmentName);
            if (hostEnvironment.IsDevelopment())
            {
                LogSeedingStarting(logger);
                var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
                await seeder.SeedAsync(stoppingToken);
                LogSeedingComplete(logger);
            }
            else
            {
                LogSeedingSkippedNotDevelopment(logger, hostEnvironment.EnvironmentName);
            }
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }

        // Stop the application once migration is complete
        hostApplicationLifetime.StopApplication();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting database migration")]
    private static partial void LogMigrationStarting(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Database migration complete")]
    private static partial void LogMigrationComplete(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Current environment: {EnvironmentName}")]
    private static partial void LogEnvironmentCheck(ILogger logger, string environmentName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting database seeding (development mode)")]
    private static partial void LogSeedingStarting(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Database seeding complete")]
    private static partial void LogSeedingComplete(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Skipping database seeding - not in development environment (current: {EnvironmentName})")]
    private static partial void LogSeedingSkippedNotDevelopment(ILogger logger, string environmentName);
}