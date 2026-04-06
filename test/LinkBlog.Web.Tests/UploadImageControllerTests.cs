using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LinkBlog.Images;
using LinkBlog.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace LinkBlog.Web.Tests;

public class UploadImageControllerTests : IDisposable
{
    private readonly Mock<BlobServiceClient> _blobServiceClient;
    private readonly Mock<BlobContainerClient> _containerClient;
    private readonly Mock<BlobClient> _blobClient;
    private readonly Mock<IImageConverter> _imageConverter;
    private readonly Mock<ILogger<UploadImageController>> _logger;
    private readonly UploadImageController _controller;

    public UploadImageControllerTests()
    {
        _blobServiceClient = new Mock<BlobServiceClient>();
        _containerClient = new Mock<BlobContainerClient>();
        _blobClient = new Mock<BlobClient>();
        _imageConverter = new Mock<IImageConverter>();
        _logger = new Mock<ILogger<UploadImageController>>();

        _blobServiceClient
            .Setup(x => x.GetBlobContainerClient("images"))
            .Returns(_containerClient.Object);

        _containerClient
            .Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Returns(_blobClient.Object);

        _controller = new UploadImageController(
            _blobServiceClient.Object,
            _imageConverter.Object,
            _logger.Object
        );
    }

    public void Dispose()
    {
        _controller.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task UploadAsync_WhenBlobCreatedSuccessfully_ReturnsOkWithFilePath()
    {
        // Arrange
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns("test-image.jpg");
        file.Setup(f => f.Length).Returns(1024);
        file.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[1024]));

        var processedImageStream = new MemoryStream(new byte[512]);
        var processedImage = new ProcessedImage(processedImageStream, "image/png", ".png");

        _imageConverter
            .Setup(x => x.ProcessImageAsync(It.IsAny<Stream>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(processedImage);

        _blobClient
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, new MockRawResponse(200)));

        var blobUri = new Uri("https://example.blob.core.windows.net/images/2025/01/01/00/00/00/test-image.png");
        _blobClient.Setup(x => x.Uri).Returns(blobUri);

        var uploadResponse = Response.FromValue(
            BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, Array.Empty<byte>(), "", 0),
            new MockRawResponse(201)
        );
        _blobClient
            .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<BlobUploadOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResponse);

        // Act
        var result = await _controller.UploadAsync(file.Object, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var filePath = doc.RootElement
            .GetProperty("data")
            .GetProperty("filePath")
            .GetString();
        Assert.Equal(blobUri.AbsoluteUri, filePath);
    }
}

// Minimal Response implementation needed to satisfy Azure SDK mock
public class MockRawResponse : Response
{
    private readonly int _status;

    public MockRawResponse(int status) => _status = status;

    public override int Status => _status;
    public override string ReasonPhrase => _status == 201 ? "Created" : "Error";
    public override Stream? ContentStream { get; set; } = new MemoryStream();
    public override string ClientRequestId { get; set; } = "";

    public override void Dispose()
    {
        GC.SuppressFinalize(this);
    }

#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member
    protected override bool TryGetHeader(string name, out string? value) { value = null; return false; }
    protected override bool TryGetHeaderValues(string name, out IEnumerable<string>? values) { values = null; return false; }
#pragma warning restore CS8765
    protected override bool ContainsHeader(string name) => false;
    protected override IEnumerable<Azure.Core.HttpHeader> EnumerateHeaders() => [];
}