using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LinkBlog.Abstractions;
using LinkBlog.Data;
using LinkBlog.Web.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace LinkBlog.Web.Tests;

[Collection("PostgreSQL")]
[Trait("Category", "UnitTest")]
public class OrphanedImageCleanupServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture fixture;
    private PostDbContext dbContext = null!;
    private string databaseName = string.Empty;
    private Mock<BlobServiceClient> mockBlobServiceClient = null!;
    private Mock<BlobContainerClient> mockContainerClient = null!;
    private Mock<ILogger<OrphanedImageCleanupService>> mockLogger = null!;
    private Mock<IDelayService> mockDelayService = null!;
    private IOptions<ImageCleanupOptions> options = null!;
    private IServiceProvider serviceProvider = null!;

    public OrphanedImageCleanupServiceTests(PostgreSqlFixture fixture)
    {
        this.fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Create unique database name using test class name and GUID
        this.databaseName = $"test_{GetType().Name}_{Guid.NewGuid():N}".ToLowerInvariant();

        // Create database with migrations applied
        this.dbContext = await this.fixture.CreateDatabaseAsync(this.databaseName);

        // Setup mocks
        this.mockBlobServiceClient = new Mock<BlobServiceClient>();
        this.mockContainerClient = new Mock<BlobContainerClient>();
        this.mockLogger = new Mock<ILogger<OrphanedImageCleanupService>>();
        this.mockDelayService = new Mock<IDelayService>();

        // Setup logger to be enabled for all log levels
        this.mockLogger
            .Setup(x => x.IsEnabled(It.IsAny<LogLevel>()))
            .Returns(true);

        // Setup delay service to return immediately (no actual delays in tests)
        this.mockDelayService
            .Setup(x => x.DelayAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Setup default options
        this.options = Options.Create(new ImageCleanupOptions
        {
            CleanupInterval = TimeSpan.FromSeconds(1),
            EnableCleanup = true,
            MinimumImageAge = TimeSpan.FromHours(1)
        });

        // Setup service provider with scoped DbContext
        var services = new ServiceCollection();
        services.AddScoped(_ => this.dbContext);
        this.serviceProvider = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        // Dispose DbContext
        await this.dbContext.DisposeAsync();

        // Drop the test database
        await this.fixture.DropDatabaseAsync(this.databaseName);

        // Dispose service provider
        if (this.serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    // ===== Constructor Tests =====

    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OrphanedImageCleanupService(
                null!,
                this.mockBlobServiceClient.Object,
                this.mockLogger.Object,
                this.mockDelayService.Object,
                this.options));
    }

    [Fact]
    public void Constructor_WithNullBlobServiceClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OrphanedImageCleanupService(
                this.serviceProvider,
                null!,
                this.mockLogger.Object,
                this.mockDelayService.Object,
                this.options));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OrphanedImageCleanupService(
                this.serviceProvider,
                this.mockBlobServiceClient.Object,
                null!,
                this.mockDelayService.Object,
                this.options));
    }

    [Fact]
    public void Constructor_WithNullDelayService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OrphanedImageCleanupService(
                this.serviceProvider,
                this.mockBlobServiceClient.Object,
                this.mockLogger.Object,
                null!,
                this.options));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OrphanedImageCleanupService(
                this.serviceProvider,
                this.mockBlobServiceClient.Object,
                this.mockLogger.Object,
                this.mockDelayService.Object,
                null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var service = new OrphanedImageCleanupService(
            this.serviceProvider,
            this.mockBlobServiceClient.Object,
            this.mockLogger.Object,
            this.mockDelayService.Object,
            this.options);

        // Assert
        Assert.NotNull(service);
    }

    // ===== ExecuteAsync Tests =====

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_DoesNotRunCleanup()
    {
        // Arrange
        var disabledOptions = Options.Create(new ImageCleanupOptions
        {
            EnableCleanup = false
        });

        var service = new OrphanedImageCleanupService(
            this.serviceProvider,
            this.mockBlobServiceClient.Object,
            this.mockLogger.Object,
            this.mockDelayService.Object,
            disabledOptions);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromMilliseconds(200));
        await service.StopAsync(CancellationToken.None);

        // Assert
        this.mockBlobServiceClient.Verify(
            x => x.GetBlobContainerClient(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_StopsGracefully()
    {
        // Arrange
        var quickOptions = Options.Create(new ImageCleanupOptions
        {
            EnableCleanup = true,
            CleanupInterval = TimeSpan.FromHours(1)
        });

        var service = new OrphanedImageCleanupService(
            this.serviceProvider,
            this.mockBlobServiceClient.Object,
            this.mockLogger.Object,
            this.mockDelayService.Object,
            quickOptions);

        using var cts = new CancellationTokenSource();

        // Act
        var executeTask = service.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromMilliseconds(100));
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        // Assert - Should complete without throwing
        await executeTask;
    }

    // ===== CleanupOrphanedImagesAsync Tests =====

    [Fact]
    public async Task CleanupOrphanedImagesAsync_WithNoOrphanedImages_DeletesNothing()
    {
        // Arrange
        var blobUrl = "https://test.blob.core.windows.net/images/test-image.jpg";
        await CreateTestPostWithImageAsync("test-post", "Test Post", blobUrl);

        var mockBlobClientForReferenced = new Mock<BlobClient>();
        mockBlobClientForReferenced
            .Setup(x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        SetupBlobStorageWithImages(blobUrl);

        var service = new OrphanedImageCleanupService(
            this.serviceProvider,
            this.mockBlobServiceClient.Object,
            this.mockLogger.Object,
            this.mockDelayService.Object,
            this.options);

        // Act
        await service.CleanupOrphanedImagesAsync(CancellationToken.None);

        // Assert - No blobs should be deleted
        mockBlobClientForReferenced.Verify(
            x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CleanupOrphanedImagesAsync_WithOrphanedImages_DeletesThem()
    {
        // Arrange
        var referencedUrl = "https://test.blob.core.windows.net/images/referenced.jpg";
        var orphanedUrl = "https://test.blob.core.windows.net/images/orphaned.jpg";

        await CreateTestPostWithImageAsync("test-post", "Test Post", referencedUrl);

        var mockOrphanedBlobClient = new Mock<BlobClient>();
        mockOrphanedBlobClient.SetupGet(x => x.Uri).Returns(new Uri(orphanedUrl));
        mockOrphanedBlobClient
            .Setup(x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        SetupBlobStorageWithImagesAndMocks(
            (referencedUrl, null),
            (orphanedUrl, mockOrphanedBlobClient.Object));

        var service = new OrphanedImageCleanupService(
            this.serviceProvider,
            this.mockBlobServiceClient.Object,
            this.mockLogger.Object,
            this.mockDelayService.Object,
            this.options);

        // Act
        await service.CleanupOrphanedImagesAsync(CancellationToken.None);

        // Assert
        mockOrphanedBlobClient.Verify(
            x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CleanupOrphanedImagesAsync_WithMultipleOrphanedImages_DeletesAllOfThem()
    {
        // Arrange
        var referencedUrl = "https://test.blob.core.windows.net/images/referenced.jpg";
        var orphaned1 = "https://test.blob.core.windows.net/images/orphaned1.jpg";
        var orphaned2 = "https://test.blob.core.windows.net/images/orphaned2.jpg";

        await CreateTestPostWithImageAsync("test-post", "Test Post", referencedUrl);

        var mockOrphanedBlobClient1 = new Mock<BlobClient>();
        mockOrphanedBlobClient1.SetupGet(x => x.Uri).Returns(new Uri(orphaned1));
        mockOrphanedBlobClient1
            .Setup(x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        var mockOrphanedBlobClient2 = new Mock<BlobClient>();
        mockOrphanedBlobClient2.SetupGet(x => x.Uri).Returns(new Uri(orphaned2));
        mockOrphanedBlobClient2
            .Setup(x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        SetupBlobStorageWithImagesAndMocks(
            (referencedUrl, null),
            (orphaned1, mockOrphanedBlobClient1.Object),
            (orphaned2, mockOrphanedBlobClient2.Object));

        var service = new OrphanedImageCleanupService(
            this.serviceProvider,
            this.mockBlobServiceClient.Object,
            this.mockLogger.Object,
            this.mockDelayService.Object,
            this.options);

        // Act
        await service.CleanupOrphanedImagesAsync(CancellationToken.None);

        // Assert
        mockOrphanedBlobClient1.Verify(
            x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        mockOrphanedBlobClient2.Verify(
            x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CleanupOrphanedImagesAsync_WhenBlobDeletionFails_ContinuesWithOtherBlobs()
    {
        // Arrange
        var orphaned1 = "https://test.blob.core.windows.net/images/orphaned1.jpg";
        var orphaned2 = "https://test.blob.core.windows.net/images/orphaned2.jpg";

        var mockOrphanedBlobClient1 = new Mock<BlobClient>();
        mockOrphanedBlobClient1.SetupGet(x => x.Uri).Returns(new Uri(orphaned1));
        mockOrphanedBlobClient1
            .Setup(x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException("Simulated deletion failure"));

        var mockOrphanedBlobClient2 = new Mock<BlobClient>();
        mockOrphanedBlobClient2.SetupGet(x => x.Uri).Returns(new Uri(orphaned2));
        mockOrphanedBlobClient2
            .Setup(x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        SetupBlobStorageWithImagesAndMocks(
            (orphaned1, mockOrphanedBlobClient1.Object),
            (orphaned2, mockOrphanedBlobClient2.Object));

        var service = new OrphanedImageCleanupService(
            this.serviceProvider,
            this.mockBlobServiceClient.Object,
            this.mockLogger.Object,
            this.mockDelayService.Object,
            this.options);

        // Act
        await service.CleanupOrphanedImagesAsync(CancellationToken.None);

        // Assert - Second blob should still be deleted even if first fails
        mockOrphanedBlobClient2.Verify(
            x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ===== CleanupOrphanedImagesAsync Minimum Age Tests =====

    [Fact]
    public async Task CleanupOrphanedImagesAsync_WithOrphanedImageYoungerThanMinimumAge_DoesNotDeleteIt()
    {
        // Arrange
        var minimumAge = TimeSpan.FromHours(2);
        var ageOptions = Options.Create(new ImageCleanupOptions
        {
            CleanupInterval = TimeSpan.FromSeconds(1),
            EnableCleanup = true,
            MinimumImageAge = minimumAge
        });

        var orphanedUrl = "https://test.blob.core.windows.net/images/new-orphaned.jpg";

        var mockOrphanedBlobClient = new Mock<BlobClient>();
        mockOrphanedBlobClient.SetupGet(x => x.Uri).Returns(new Uri(orphanedUrl));
        mockOrphanedBlobClient
            .Setup(x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        // Create blob that is younger than minimum age (30 minutes old, but minimum is 2 hours)
        var blobCreationTime = DateTimeOffset.UtcNow.AddMinutes(-30);
        SetupBlobStorageWithImagesAndCreationTimes(
            (orphanedUrl, mockOrphanedBlobClient.Object, blobCreationTime));

        var service = new OrphanedImageCleanupService(
            this.serviceProvider,
            this.mockBlobServiceClient.Object,
            this.mockLogger.Object,
            this.mockDelayService.Object,
            ageOptions);

        // Act
        await service.CleanupOrphanedImagesAsync(CancellationToken.None);

        // Assert - Blob should NOT be deleted because it's too new
        mockOrphanedBlobClient.Verify(
            x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CleanupOrphanedImagesAsync_WithOrphanedImageOlderThanMinimumAge_DeletesIt()
    {
        // Arrange
        var minimumAge = TimeSpan.FromHours(2);
        var ageOptions = Options.Create(new ImageCleanupOptions
        {
            CleanupInterval = TimeSpan.FromSeconds(1),
            EnableCleanup = true,
            MinimumImageAge = minimumAge
        });

        var orphanedUrl = "https://test.blob.core.windows.net/images/old-orphaned.jpg";

        var mockOrphanedBlobClient = new Mock<BlobClient>();
        mockOrphanedBlobClient.SetupGet(x => x.Uri).Returns(new Uri(orphanedUrl));
        mockOrphanedBlobClient
            .Setup(x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        // Create blob that is older than minimum age (5 hours old, minimum is 2 hours)
        var blobCreationTime = DateTimeOffset.UtcNow.AddHours(-5);
        SetupBlobStorageWithImagesAndCreationTimes(
            (orphanedUrl, mockOrphanedBlobClient.Object, blobCreationTime));

        var service = new OrphanedImageCleanupService(
            this.serviceProvider,
            this.mockBlobServiceClient.Object,
            this.mockLogger.Object,
            this.mockDelayService.Object,
            ageOptions);

        // Act
        await service.CleanupOrphanedImagesAsync(CancellationToken.None);

        // Assert - Blob SHOULD be deleted because it's old enough
        mockOrphanedBlobClient.Verify(
            x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CleanupOrphanedImagesAsync_WithMixOfOldAndNewOrphanedImages_DeletesOnlyOldOnes()
    {
        // Arrange
        var minimumAge = TimeSpan.FromHours(2);
        var ageOptions = Options.Create(new ImageCleanupOptions
        {
            CleanupInterval = TimeSpan.FromSeconds(1),
            EnableCleanup = true,
            MinimumImageAge = minimumAge
        });

        var newOrphanedUrl = "https://test.blob.core.windows.net/images/new-orphaned.jpg";
        var oldOrphanedUrl = "https://test.blob.core.windows.net/images/old-orphaned.jpg";

        var mockNewBlobClient = new Mock<BlobClient>();
        mockNewBlobClient.SetupGet(x => x.Uri).Returns(new Uri(newOrphanedUrl));
        mockNewBlobClient
            .Setup(x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        var mockOldBlobClient = new Mock<BlobClient>();
        mockOldBlobClient.SetupGet(x => x.Uri).Returns(new Uri(oldOrphanedUrl));
        mockOldBlobClient
            .Setup(x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        // Create one new blob (30 minutes old) and one old blob (5 hours old)
        var newBlobCreationTime = DateTimeOffset.UtcNow.AddMinutes(-30);
        var oldBlobCreationTime = DateTimeOffset.UtcNow.AddHours(-5);
        SetupBlobStorageWithImagesAndCreationTimes(
            (newOrphanedUrl, mockNewBlobClient.Object, newBlobCreationTime),
            (oldOrphanedUrl, mockOldBlobClient.Object, oldBlobCreationTime));

        var service = new OrphanedImageCleanupService(
            this.serviceProvider,
            this.mockBlobServiceClient.Object,
            this.mockLogger.Object,
            this.mockDelayService.Object,
            ageOptions);

        // Act
        await service.CleanupOrphanedImagesAsync(CancellationToken.None);

        // Assert - Only old blob should be deleted
        mockNewBlobClient.Verify(
            x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()),
            Times.Never,
            "New blob should not be deleted");

        mockOldBlobClient.Verify(
            x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Old blob should be deleted");
    }

    [Fact]
    public async Task CleanupOrphanedImagesAsync_WithImageAtExactlyMinimumAge_DeletesIt()
    {
        // Arrange
        var minimumAge = TimeSpan.FromHours(2);
        var ageOptions = Options.Create(new ImageCleanupOptions
        {
            CleanupInterval = TimeSpan.FromSeconds(1),
            EnableCleanup = true,
            MinimumImageAge = minimumAge
        });

        var orphanedUrl = "https://test.blob.core.windows.net/images/exact-age-orphaned.jpg";

        var mockOrphanedBlobClient = new Mock<BlobClient>();
        mockOrphanedBlobClient.SetupGet(x => x.Uri).Returns(new Uri(orphanedUrl));
        mockOrphanedBlobClient
            .Setup(x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        // Create blob that is exactly at the minimum age (2 hours old)
        var blobCreationTime = DateTimeOffset.UtcNow.Add(-minimumAge);
        SetupBlobStorageWithImagesAndCreationTimes(
            (orphanedUrl, mockOrphanedBlobClient.Object, blobCreationTime));

        var service = new OrphanedImageCleanupService(
            this.serviceProvider,
            this.mockBlobServiceClient.Object,
            this.mockLogger.Object,
            this.mockDelayService.Object,
            ageOptions);

        // Act
        await service.CleanupOrphanedImagesAsync(CancellationToken.None);

        // Assert - Blob SHOULD be deleted because the condition is >= minimum age
        mockOrphanedBlobClient.Verify(
            x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ===== CleanupOrphanedImagesAsync Dry Run Tests =====

    [Fact]
    public async Task CleanupOrphanedImagesAsync_WithDryRunEnabled_DoesNotDeleteBlobs()
    {
        // Arrange
        var dryRunOptions = Options.Create(new ImageCleanupOptions
        {
            CleanupInterval = TimeSpan.FromSeconds(1),
            EnableCleanup = true,
            MinimumImageAge = TimeSpan.FromHours(1),
            DryRun = true
        });

        var orphanedUrl = "https://test.blob.core.windows.net/images/orphaned.jpg";

        var mockOrphanedBlobClient = new Mock<BlobClient>();
        mockOrphanedBlobClient.SetupGet(x => x.Uri).Returns(new Uri(orphanedUrl));
        mockOrphanedBlobClient
            .Setup(x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        SetupBlobStorageWithImagesAndMocks((orphanedUrl, mockOrphanedBlobClient.Object));

        var service = new OrphanedImageCleanupService(
            this.serviceProvider,
            this.mockBlobServiceClient.Object,
            this.mockLogger.Object,
            this.mockDelayService.Object,
            dryRunOptions);

        // Act
        await service.CleanupOrphanedImagesAsync(CancellationToken.None);

        // Assert - Blob should NOT be deleted in dry run mode
        mockOrphanedBlobClient.Verify(
            x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()),
            Times.Never,
            "Blobs should not be deleted in dry run mode");
    }

    [Fact]
    public async Task CleanupOrphanedImagesAsync_WithDryRunEnabled_LogsWhatWouldBeDeleted()
    {
        // Arrange
        var dryRunOptions = Options.Create(new ImageCleanupOptions
        {
            CleanupInterval = TimeSpan.FromSeconds(1),
            EnableCleanup = true,
            MinimumImageAge = TimeSpan.FromHours(1),
            DryRun = true
        });

        var orphanedUrl = "https://test.blob.core.windows.net/images/orphaned.jpg";

        var mockOrphanedBlobClient = new Mock<BlobClient>();
        mockOrphanedBlobClient.SetupGet(x => x.Uri).Returns(new Uri(orphanedUrl));

        SetupBlobStorageWithImagesAndMocks((orphanedUrl, mockOrphanedBlobClient.Object));

        var service = new OrphanedImageCleanupService(
            this.serviceProvider,
            this.mockBlobServiceClient.Object,
            this.mockLogger.Object,
            this.mockDelayService.Object,
            dryRunOptions);

        // Act
        await service.CleanupOrphanedImagesAsync(CancellationToken.None);

        // Assert - Verify dry run log messages were generated
#pragma warning disable CA1873 // Evaluation of this argument may be expensive in production, but we're in test code
        this.mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[DRY RUN] Would delete orphaned image: orphaned.jpg")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log what would be deleted in dry run mode");

        this.mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[DRY RUN] Orphaned image cleanup completed. Would have deleted 1 of 1 orphaned images")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log dry run completion summary");
#pragma warning restore CA1873
    }

    [Fact]
    public async Task CleanupOrphanedImagesAsync_WithDryRunDisabled_DeletesBlobsNormally()
    {
        // Arrange
        var normalOptions = Options.Create(new ImageCleanupOptions
        {
            CleanupInterval = TimeSpan.FromSeconds(1),
            EnableCleanup = true,
            MinimumImageAge = TimeSpan.FromHours(1),
            DryRun = false
        });

        var orphanedUrl = "https://test.blob.core.windows.net/images/orphaned.jpg";

        var mockOrphanedBlobClient = new Mock<BlobClient>();
        mockOrphanedBlobClient.SetupGet(x => x.Uri).Returns(new Uri(orphanedUrl));
        mockOrphanedBlobClient
            .Setup(x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        SetupBlobStorageWithImagesAndMocks((orphanedUrl, mockOrphanedBlobClient.Object));

        var service = new OrphanedImageCleanupService(
            this.serviceProvider,
            this.mockBlobServiceClient.Object,
            this.mockLogger.Object,
            this.mockDelayService.Object,
            normalOptions);

        // Act
        await service.CleanupOrphanedImagesAsync(CancellationToken.None);

        // Assert - Blob SHOULD be deleted when dry run is disabled
        mockOrphanedBlobClient.Verify(
            x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Blobs should be deleted when dry run is disabled");
    }

    [Fact]
    public async Task CleanupOrphanedImagesAsync_WithDryRunEnabledAndMultipleOrphanedImages_LogsAllThatWouldBeDeleted()
    {
        // Arrange
        var dryRunOptions = Options.Create(new ImageCleanupOptions
        {
            CleanupInterval = TimeSpan.FromSeconds(1),
            EnableCleanup = true,
            MinimumImageAge = TimeSpan.FromHours(1),
            DryRun = true
        });

        var orphaned1 = "https://test.blob.core.windows.net/images/orphaned1.jpg";
        var orphaned2 = "https://test.blob.core.windows.net/images/orphaned2.jpg";
        var orphaned3 = "https://test.blob.core.windows.net/images/orphaned3.jpg";

        var mockOrphanedBlobClient1 = new Mock<BlobClient>();
        mockOrphanedBlobClient1.SetupGet(x => x.Uri).Returns(new Uri(orphaned1));

        var mockOrphanedBlobClient2 = new Mock<BlobClient>();
        mockOrphanedBlobClient2.SetupGet(x => x.Uri).Returns(new Uri(orphaned2));

        var mockOrphanedBlobClient3 = new Mock<BlobClient>();
        mockOrphanedBlobClient3.SetupGet(x => x.Uri).Returns(new Uri(orphaned3));

        SetupBlobStorageWithImagesAndMocks(
            (orphaned1, mockOrphanedBlobClient1.Object),
            (orphaned2, mockOrphanedBlobClient2.Object),
            (orphaned3, mockOrphanedBlobClient3.Object));

        var service = new OrphanedImageCleanupService(
            this.serviceProvider,
            this.mockBlobServiceClient.Object,
            this.mockLogger.Object,
            this.mockDelayService.Object,
            dryRunOptions);

        // Act
        await service.CleanupOrphanedImagesAsync(CancellationToken.None);

        // Assert - Verify all images are logged but none are deleted
#pragma warning disable CA1873 // Evaluation of this argument may be expensive in production, but we're in test code
        this.mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[DRY RUN] Would delete orphaned image:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3),
            "Should log all 3 images that would be deleted");

        this.mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[DRY RUN] Orphaned image cleanup completed. Would have deleted 3 of 3 orphaned images")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log dry run completion with correct count");
#pragma warning restore CA1873

        // Verify no actual deletions occurred
        mockOrphanedBlobClient1.Verify(
            x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        mockOrphanedBlobClient2.Verify(
            x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        mockOrphanedBlobClient3.Verify(
            x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ===== GetAllBlobUrlsAsync Tests =====

    [Fact]
    public async Task GetAllBlobUrlsAsync_WhenContainerDoesNotExist_ReturnsEmptySet()
    {
        // Arrange
        this.mockContainerClient
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

        this.mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient("images"))
            .Returns(this.mockContainerClient.Object);

        var service = new OrphanedImageCleanupService(
            this.serviceProvider,
            this.mockBlobServiceClient.Object,
            this.mockLogger.Object,
            this.mockDelayService.Object,
            this.options);

        // Act
        var result = await service.GetAllBlobUrlsAsync(CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    // ===== GetReferencedImageUrlsAsync Tests =====

    [Fact]
    public async Task GetReferencedImageUrlsAsync_WithPostsContainingImages_ExtractsUrls()
    {
        // Arrange
        var imageUrl1 = "https://test.blob.core.windows.net/images/image1.jpg";
        var imageUrl2 = "https://test.blob.core.windows.net/images/image2.png";

        await CreateTestPostWithImageAsync("post-1", "Post 1", imageUrl1);
        await CreateTestPostWithImageAsync("post-2", "Post 2", imageUrl2);

        var service = new OrphanedImageCleanupService(
            this.serviceProvider,
            this.mockBlobServiceClient.Object,
            this.mockLogger.Object,
            this.mockDelayService.Object,
            this.options);

        // Act
        var result = await service.GetReferencedImageUrlsAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(imageUrl1, result);
        Assert.Contains(imageUrl2, result);
    }

    [Fact]
    public async Task GetReferencedImageUrlsAsync_WithPostsContainingMultipleImages_ExtractsAllUrls()
    {
        // Arrange
        var imageUrl1 = "https://test.blob.core.windows.net/images/image1.jpg";
        var imageUrl2 = "https://test.blob.core.windows.net/images/image2.png";

        var contents = $"This post has two images: {imageUrl1} and {imageUrl2}";
        await CreateTestPostWithImageAsync("post-1", "Post 1", contents);

        var service = new OrphanedImageCleanupService(
            this.serviceProvider,
            this.mockBlobServiceClient.Object,
            this.mockLogger.Object,
            this.mockDelayService.Object,
            this.options);

        // Act
        var result = await service.GetReferencedImageUrlsAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(imageUrl1, result);
        Assert.Contains(imageUrl2, result);
    }

    [Fact]
    public async Task GetReferencedImageUrlsAsync_WithArchivedPosts_IncludesTheirImages()
    {
        // Arrange
        var imageUrl = "https://test.blob.core.windows.net/images/archived-image.jpg";
        var postId = await CreateTestPostWithImageAsync("archived-post", "Archived Post", imageUrl);

        // Archive the post
        var postStore = new PostStoreDb(this.dbContext);
        await postStore.ArchivePostAsync(postId);

        var service = new OrphanedImageCleanupService(
            this.serviceProvider,
            this.mockBlobServiceClient.Object,
            this.mockLogger.Object,
            this.mockDelayService.Object,
            this.options);

        // Act
        var result = await service.GetReferencedImageUrlsAsync(CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Contains(imageUrl, result);
    }

    [Fact]
    public async Task GetReferencedImageUrlsAsync_WithEmptyContents_ReturnsEmptySet()
    {
        // Arrange
        var post = new Post
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Empty Post",
            ShortTitle = "empty-post",
            Contents = string.Empty,
            CreatedDate = DateTimeOffset.UtcNow,
            LastUpdatedDate = DateTimeOffset.UtcNow
        };

        var postStore = new PostStoreDb(this.dbContext);
        await postStore.CreatePostAsync(post, new List<string>());

        var service = new OrphanedImageCleanupService(
            this.serviceProvider,
            this.mockBlobServiceClient.Object,
            this.mockLogger.Object,
            this.mockDelayService.Object,
            this.options);

        // Act
        var result = await service.GetReferencedImageUrlsAsync(CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetReferencedImageUrlsAsync_WithVariousImageUrlFormats_ExtractsAllFormats()
    {
        // Arrange
        var httpUrl = "http://test.blob.core.windows.net/images/http-image.jpg";
        var httpsUrl = "https://test.blob.core.windows.net/images/https-image.jpg";
        var gifUrl = "https://test.blob.core.windows.net/images/animated.gif";

        var contents = $"Images: {httpUrl} {httpsUrl} {gifUrl}";
        await CreateTestPostWithImageAsync("post-1", "Post 1", contents);

        var service = new OrphanedImageCleanupService(
            this.serviceProvider,
            this.mockBlobServiceClient.Object,
            this.mockLogger.Object,
            this.mockDelayService.Object,
            this.options);

        // Act
        var result = await service.GetReferencedImageUrlsAsync(CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(httpUrl, result);
        Assert.Contains(httpsUrl, result);
        Assert.Contains(gifUrl, result);
    }

    [Fact]
    public async Task GetReferencedImageUrlsAsync_IsCaseInsensitive()
    {
        // Arrange
        var imageUrl = "https://test.blob.core.windows.net/images/Test-Image.JPG";
        await CreateTestPostWithImageAsync("post-1", "Post 1", imageUrl);

        var service = new OrphanedImageCleanupService(
            this.serviceProvider,
            this.mockBlobServiceClient.Object,
            this.mockLogger.Object,
            this.mockDelayService.Object,
            this.options);

        // Act
        var result = await service.GetReferencedImageUrlsAsync(CancellationToken.None);

        // Assert
        Assert.Single(result);
        // Verify case-insensitive comparison works
        Assert.Contains(result, url => url.Equals(imageUrl, StringComparison.OrdinalIgnoreCase));
    }

    // ===== Helper Methods =====

    private async Task<string> CreateTestPostWithImageAsync(string shortTitle, string title, string imageUrlOrContents)
    {
        var post = new Post
        {
            Id = Guid.NewGuid().ToString(),
            Title = title,
            ShortTitle = shortTitle,
            Contents = $"In this test post is an image <img src=\"{imageUrlOrContents}\">",
            CreatedDate = DateTimeOffset.UtcNow,
            LastUpdatedDate = DateTimeOffset.UtcNow
        };

        var postStore = new PostStoreDb(this.dbContext);
        await postStore.CreatePostAsync(post, new List<string>());
        return post.Id;
    }

    private void SetupBlobStorageWithImages(params string[] blobUrls)
    {
        var items = blobUrls.Select(url => (url, (BlobClient?)null)).ToArray();
        SetupBlobStorageWithImagesAndMocks(items);
    }

    private void SetupBlobStorageWithImagesAndMocks(params (string url, BlobClient? mockClient)[] blobs)
    {
        var blobItems = new List<BlobItem>();

        foreach (var (url, mockClient) in blobs)
        {
            var uri = new Uri(url);
            var pathParts = uri.AbsolutePath.Split("/images/", 2, StringSplitOptions.RemoveEmptyEntries);
            var blobName = pathParts.Length > 0 ? pathParts[^1] : uri.Segments[^1];
            var properties = BlobsModelFactory.BlobItemProperties(true, createdOn: DateTimeOffset.UtcNow.AddHours(-3));

            blobItems.Add(BlobsModelFactory.BlobItem(name: blobName, properties: properties));

            // Use provided mock client or create a real BlobClient instance
            var blobClient = mockClient ?? new BlobClient(uri);

            this.mockContainerClient
                .Setup(x => x.GetBlobClient(blobName))
                .Returns(blobClient);
        }

        this.mockContainerClient
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        this.mockContainerClient
            .Setup(x => x.GetBlobsAsync(
                It.IsAny<BlobTraits>(),
                It.IsAny<BlobStates>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable(blobItems));

        this.mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient("images"))
            .Returns(this.mockContainerClient.Object);
    }

    private void SetupBlobStorageWithImagesAndCreationTimes(params (string url, BlobClient? mockClient, DateTimeOffset createdOn)[] blobs)
    {
        var blobItems = new List<BlobItem>();

        foreach (var (url, mockClient, createdOn) in blobs)
        {
            var uri = new Uri(url);
            var pathParts = uri.AbsolutePath.Split("/images/", 2, StringSplitOptions.RemoveEmptyEntries);
            var blobName = pathParts.Length > 0 ? pathParts[^1] : uri.Segments[^1];
            var properties = BlobsModelFactory.BlobItemProperties(true, createdOn: createdOn);

            blobItems.Add(BlobsModelFactory.BlobItem(name: blobName, properties: properties));

            // Use provided mock client or create a real BlobClient instance
            var blobClient = mockClient ?? new BlobClient(uri);

            this.mockContainerClient
                .Setup(x => x.GetBlobClient(blobName))
                .Returns(blobClient);
        }

        this.mockContainerClient
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        this.mockContainerClient
            .Setup(x => x.GetBlobsAsync(
                It.IsAny<BlobTraits>(),
                It.IsAny<BlobStates>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable(blobItems));

        this.mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient("images"))
            .Returns(this.mockContainerClient.Object);
    }

    // Helper class to mock AsyncPageable<BlobItem>
    private sealed class MockAsyncPageable : AsyncPageable<BlobItem>
    {
        private readonly List<BlobItem> items;

        public MockAsyncPageable(List<BlobItem> items)
        {
            this.items = items;
        }

        public override async IAsyncEnumerable<Page<BlobItem>> AsPages(string? continuationToken = null, int? pageSizeHint = null)
        {
            await Task.Yield();
            yield return Page<BlobItem>.FromValues(this.items, null, Mock.Of<Response>());
        }
    }
}