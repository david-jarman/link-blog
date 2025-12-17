using LinkBlog.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace LinkBlog.Web.Tests.Fixtures;

public class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgreSqlContainer;

    public PostgreSqlFixture()
    {
        this.postgreSqlContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("linkblog_test")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .WithCleanUp(true)
            .Build();
    }

    public string ConnectionString => this.postgreSqlContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await this.postgreSqlContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await this.postgreSqlContainer.DisposeAsync();
    }

    /// <summary>
    /// Creates a new database with a unique name and applies all migrations.
    /// Returns a DbContext configured for that database.
    /// </summary>
    public async Task<PostDbContext> CreateDatabaseAsync(string databaseName)
    {
        // Create a context connected to the default test database
        var optionsBuilder = new DbContextOptionsBuilder<PostDbContext>();
        optionsBuilder.UseNpgsql(this.ConnectionString);

        using (var setupContext = new PostDbContext(optionsBuilder.Options))
        {
            // Create the new database
            // Database names cannot be parameterized; using quoted identifier for safety
#pragma warning disable EF1002 // Risk of SQL injection
            await setupContext.Database.ExecuteSqlRawAsync($"CREATE DATABASE \"{databaseName}\"");
#pragma warning restore EF1002
        }

        // Build connection string for the new database
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(this.ConnectionString)
        {
            Database = databaseName
        };

        // Create context for the new database and apply migrations
        var newOptionsBuilder = new DbContextOptionsBuilder<PostDbContext>();
        newOptionsBuilder.UseNpgsql(builder.ConnectionString);

        var context = new PostDbContext(newOptionsBuilder.Options);
        await context.Database.MigrateAsync();

        return context;
    }

    /// <summary>
    /// Drops a database created by CreateDatabaseAsync.
    /// </summary>
    public async Task DropDatabaseAsync(string databaseName)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PostDbContext>();
        optionsBuilder.UseNpgsql(this.ConnectionString);

        using var context = new PostDbContext(optionsBuilder.Options);

        // Terminate connections to the database before dropping
        // Database names cannot be parameterized; using safe identifier quoting
#pragma warning disable EF1002 // Risk of SQL injection
        await context.Database.ExecuteSqlRawAsync($@"
            SELECT pg_terminate_backend(pg_stat_activity.pid)
            FROM pg_stat_activity
            WHERE pg_stat_activity.datname = '{databaseName}'
            AND pid <> pg_backend_pid()");

        await context.Database.ExecuteSqlRawAsync($"DROP DATABASE IF EXISTS \"{databaseName}\"");
#pragma warning restore EF1002
    }
}