# EasyMDE Image Upload Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire EasyMDE's built-in image upload button to the existing `/api/upload` endpoint so images can be uploaded directly from the post editor.

**Architecture:** Change the upload controller's success response from `201 Created` (empty body) to `200 OK` with `{"data": {"filePath": "<url>"}}` — the format EasyMDE's `imageUploadEndpoint` expects. Then configure EasyMDE with that endpoint.

**Tech Stack:** ASP.NET Core MVC (C#), EasyMDE (JavaScript), xUnit + Moq (tests)

---

### Task 1: Update `UploadImageController` response format

**Files:**
- Modify: `src/LinkBlog.Web/Controllers/UploadImageController.cs:79`
- Create: `test/LinkBlog.Web.Tests/UploadImageControllerTests.cs`

- [ ] **Step 1: Write the failing test**

Create `test/LinkBlog.Web.Tests/UploadImageControllerTests.cs`:

```csharp
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LinkBlog.Images;
using LinkBlog.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace LinkBlog.Web.Tests;

public class UploadImageControllerTests
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
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

        var blobUri = new Uri("https://example.blob.core.windows.net/images/2025/01/01/00/00/00/test-image.png");
        _blobClient.Setup(x => x.Uri).Returns(blobUri);

        var uploadResponse = Response.FromValue(
            BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, new byte[0], "", 0),
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
    public override void Dispose() { }
    protected override bool TryGetHeader(string name, out string? value) { value = null; return false; }
    protected override bool TryGetHeaderValues(string name, out IEnumerable<string>? values) { values = null; return false; }
    protected override bool ContainsHeader(string name) => false;
    protected override IEnumerable<HttpHeader> EnumerateHeaders() => [];
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test test/LinkBlog.Web.Tests/LinkBlog.Web.Tests.csproj --filter "UploadImageControllerTests" -v normal
```

Expected: FAIL — test exists but `OkObjectResult` assertion fails because controller currently returns `CreatedResult`.

- [ ] **Step 3: Change the controller response**

In `src/LinkBlog.Web/Controllers/UploadImageController.cs`, replace line 79:

```csharp
return Created(blobClient.Uri.AbsoluteUri, null);
```

With:

```csharp
return Ok(new { data = new { filePath = blobClient.Uri.AbsoluteUri } });
```

- [ ] **Step 4: Run test to verify it passes**

```bash
dotnet test test/LinkBlog.Web.Tests/LinkBlog.Web.Tests.csproj --filter "UploadImageControllerTests" -v normal
```

Expected: PASS

- [ ] **Step 5: Run all tests to check for regressions**

```bash
dotnet test test/LinkBlog.Web.Tests/LinkBlog.Web.Tests.csproj -v normal
```

Expected: all tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/LinkBlog.Web/Controllers/UploadImageController.cs test/LinkBlog.Web.Tests/UploadImageControllerTests.cs
git commit -m "feat: change upload endpoint response format to match EasyMDE expectations"
```

---

### Task 2: Configure EasyMDE with image upload

**Files:**
- Modify: `src/LinkBlog.Web/Components/Pages/Admin/AdminHome.razor:18-26`

- [ ] **Step 1: Add image upload options to EasyMDE initializer**

In `src/LinkBlog.Web/Components/Pages/Admin/AdminHome.razor`, update the `new EasyMDE({...})` block (currently lines 18–26) to add three new options:

```js
document.addEventListener('DOMContentLoaded', function () {
    var easyMDE = new EasyMDE({
        element: document.getElementById("PostContent"),
        spellChecker: false,
        autosave: {
            enabled: true,
            uniqueId: "admin-post-editor",
            delay: 1000,
        },
        imageUploadEndpoint: "/api/upload",
        imagePathAbsolute: true,
        imageAccept: "image/png, image/jpeg, image/gif, image/webp",
    });
});
```

- [ ] **Step 2: Build to verify no compilation errors**

```bash
dotnet build src/LinkBlog.Web/LinkBlog.Web.csproj
```

Expected: Build succeeded, 0 Error(s).

- [ ] **Step 3: Manual smoke test**

Start the app with `aspire run`. Navigate to `/admin`. Confirm:
- The EasyMDE toolbar has an image icon (camera icon, tooltip "Upload Image").
- Clicking it opens a file picker.
- Selecting an image uploads it and inserts `![](https://...)` markdown into the editor.

- [ ] **Step 4: Commit**

```bash
git add src/LinkBlog.Web/Components/Pages/Admin/AdminHome.razor
git commit -m "feat: enable image upload in EasyMDE editor"
```
