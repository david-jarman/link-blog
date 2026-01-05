using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LinkBlog.Images;
using LinkBlog.Web.Logging;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkBlog.Web.Controllers;

[Route("api")]
[Authorize(Policy = "Admin")]
public class UploadImageController : Controller
{
    private readonly BlobServiceClient blobServiceClient;
    private readonly IImageConverter imageConverter;
    private readonly ILogger<UploadImageController> logger;

    public UploadImageController(BlobServiceClient blobServiceClient, IImageConverter imageConverter, ILogger<UploadImageController> logger)
    {
        this.blobServiceClient = blobServiceClient;
        this.imageConverter = imageConverter;
        this.logger = logger;
    }

    [HttpPost("upload")]
    [RequireAntiforgeryToken(required: false)]
    public async Task<ActionResult> UploadAsync(IFormFile file, CancellationToken ct)
    {
        if (file is null)
        {
            logger.NoFileUploaded();
            return BadRequest();
        }

        // Get or create the container first. Container name is "images".
        var containerClient = blobServiceClient.GetBlobContainerClient("images");
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.FileName);

        // Load and process the image from the stream
        using Stream originalImage = file.OpenReadStream();
        using var processedImage = await imageConverter.ProcessImageAsync(originalImage, file.Length, ct);

        // Blob path should be prefixed with the current datetime to ensure uniqueness.
        // Example: "2025/08/01/12/00/00/imagename.png" or "2025/08/01/12/00/00/imagename.gif"
        string blobPath = $"{DateTimeOffset.UtcNow:yyyy/MM/dd/HH/mm/ss}/{fileNameWithoutExtension}{processedImage.FileExtension}";

        var blobClient = containerClient.GetBlobClient(blobPath);

        // Check if the blob already exists. If it does, return a 409 Conflict.
        if (await blobClient.ExistsAsync(ct))
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Blob {BlobPath} already exists.", blobPath);
            }
            return Conflict();
        }

        var blobUploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = processedImage.ContentType
            }
        };

        var response = await blobClient.UploadAsync(processedImage.Stream, blobUploadOptions, ct);

        // Check if the response was successful. If it was, return the permanent url to the blob.
        if (response.GetRawResponse().Status == StatusCodes.Status201Created)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Blob {BlobPath} successfully created", blobPath);
            }
            return Created(blobClient.Uri.AbsoluteUri, null);
        }
        else
        {
            // If the response was not successful, return a 500 Internal Server Error.
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}