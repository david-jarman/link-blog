using LinkBlog.Data;
using LinkBlog.Web.Tests.Fixtures;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace LinkBlog.Web.Tests.Integration;

[Collection("PostgreSQL")]
[Trait("Category", "IntegrationTest")]
public abstract class PostStoreDbTestBase : IAsyncLifetime, IDisposable
{
    protected PostgreSqlFixture Fixture { get; }
    protected PostDbContext DbContext { get; private set; } = null!;
    protected IPostStore PostStore { get; private set; } = null!;

    private string databaseName = string.Empty;
    private MemoryCache? memoryCache;
    private CachedPostStore? cachedPostStore;

    protected PostStoreDbTestBase(PostgreSqlFixture fixture)
    {
        this.Fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Create unique database name using test class name and GUID
        this.databaseName = $"test_{GetType().Name}_{Guid.NewGuid():N}".ToLowerInvariant();

        // Create database with migrations applied
        this.DbContext = await this.Fixture.CreateDatabaseAsync(this.databaseName);

        // Create the data access layer
        var dataAccess = new PostDataAccess(this.DbContext);

        // Create memory cache for the cached post store
        this.memoryCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });

        // Create the CachedPostStore which implements IPostStore
        this.cachedPostStore = new CachedPostStore(
            dataAccess,
            this.memoryCache,
            NullLogger<CachedPostStore>.Instance);

        this.PostStore = this.cachedPostStore;
    }

    public async Task DisposeAsync()
    {
        // Dispose DbContext
        await this.DbContext.DisposeAsync();

        // Drop the test database
        await this.Fixture.DropDatabaseAsync(this.databaseName);
    }

    public void Dispose()
    {
        // Dispose CachedPostStore
        this.cachedPostStore?.Dispose();

        // Dispose memory cache
        this.memoryCache?.Dispose();

        GC.SuppressFinalize(this);
    }
}
